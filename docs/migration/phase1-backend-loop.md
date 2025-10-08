# Faza 1: ApplicationOrchestrator - Backend Main Loop

**Data utworzenia:** 2025-10-08
**Status:** Zaakceptowany

---

## Przegląd

`ApplicationOrchestrator` to **główna pętla backendu** (main loop), która koordynuje wszystkie operacje w AgOpenGPS. Zastępuje ona obecny timer `FormGPS.tmrWatchdog` i przenosi kontrolę nad timingiem aplikacji z frontendu do backendu.

**Kluczowe cechy:**
- **Frequency:** 10 Hz (100ms interval) - zwiększone z obecnych 4 Hz (250ms)
- **Hosted Service:** Implementuje `IHostedService` (ASP.NET Core)
- **Coordination:** Wywołuje services w określonej kolejności
- **Push Updates:** Broadcastuje state przez SignalR do wszystkich frontendów

---

## Obecny stan (FormGPS)

### FormGPS.cs - Timer Watchdog (250ms)

```csharp
// GPS/Forms/FormGPS.cs (istniejący kod)

private void tmrWatchdog_Tick(object sender, EventArgs e)
{
    // 250ms = 4 Hz

    // 1. Update vehicle position from GNSS
    pn.UpdatePosition();

    // 2. Calculate guidance
    guidanceLine.UpdateGuidanceLine();

    // 3. Update section control
    section.UpdateSections();

    // 4. Update autosteer
    if (isAutoSteerBtnOn)
    {
        guidanceLine.CalculateSteerAngle();
        mc.SendAutoSteerData();
    }

    // 5. Update UI
    lblSpeed.Text = pn.speed.ToString("F1");
    lblHeading.Text = pn.heading.ToString("F1");
    // ... dozens more UI updates

    // 6. Redraw OpenGL
    oglMain.Invalidate();
}
```

**Problemy:**
- ❌ UI logic mixed with business logic
- ❌ Frontend kontroluje timing
- ❌ Trudno testować (wymaga FormGPS)
- ❌ Niemożliwe użycie bez WinForms
- ❌ 250ms interval może być za wolny dla precision guidance

---

## Nowy stan (ApplicationOrchestrator)

### Implementacja

```csharp
// AgOpenGPS.Api/Orchestration/ApplicationOrchestrator.cs

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgOpenGPS.Api.Orchestration
{
    public class ApplicationOrchestrator : IHostedService, IDisposable
    {
        private readonly IHubContext<StateHub> _hubContext;
        private readonly IGnssService _gnssService;
        private readonly IVehicleService _vehicleService;
        private readonly IGuidanceService _guidanceService;
        private readonly ISectionControlService _sectionService;
        private readonly IAutoSteerService _autoSteerService;
        private readonly IFieldService _fieldService;
        private readonly ILogger<ApplicationOrchestrator> _logger;
        private readonly OrchestrationConfig _config;

        private Timer _mainLoopTimer;
        private int _tickCounter = 0;
        private DateTime _lastTickTime;

        public ApplicationOrchestrator(
            IHubContext<StateHub> hubContext,
            IGnssService gnssService,
            IVehicleService vehicleService,
            IGuidanceService guidanceService,
            ISectionControlService sectionService,
            IAutoSteerService autoSteerService,
            IFieldService fieldService,
            ILogger<ApplicationOrchestrator> logger,
            OrchestrationConfig config)
        {
            _hubContext = hubContext;
            _gnssService = gnssService;
            _vehicleService = vehicleService;
            _guidanceService = guidanceService;
            _sectionService = sectionService;
            _autoSteerService = autoSteerService;
            _fieldService = fieldService;
            _logger = logger;
            _config = config;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "ApplicationOrchestrator starting. Main loop: {Frequency} Hz ({Interval} ms)",
                _config.MainLoopFrequencyHz,
                _config.MainLoopIntervalMs);

            _lastTickTime = DateTime.UtcNow;

            _mainLoopTimer = new Timer(
                MainLoopTick,
                null,
                TimeSpan.Zero,              // Start immediately
                TimeSpan.FromMilliseconds(_config.MainLoopIntervalMs));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ApplicationOrchestrator stopping");

            _mainLoopTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void MainLoopTick(object state)
        {
            var startTime = DateTime.UtcNow;
            _tickCounter++;

            try
            {
                // Measure loop timing
                var deltaTime = (startTime - _lastTickTime).TotalMilliseconds;
                _lastTickTime = startTime;

                if (deltaTime > _config.MainLoopIntervalMs * 1.5)
                {
                    _logger.LogWarning(
                        "Main loop tick #{Tick} delayed: {DeltaMs:F1} ms (expected {ExpectedMs} ms)",
                        _tickCounter, deltaTime, _config.MainLoopIntervalMs);
                }

                // ============================================================
                // MAIN LOOP SEQUENCE (order matters!)
                // ============================================================

                // 1. HARDWARE: Read GNSS data
                var gnssData = await _gnssService.GetLatestPositionAsync();

                // 2. VEHICLE: Update vehicle state from GNSS
                var vehicleState = await _vehicleService.UpdateStateAsync(gnssData);

                // 3. GUIDANCE: Calculate guidance (distance from AB line, steer angle)
                var guidanceState = await _guidanceService.UpdateAsync(vehicleState);

                // 4. SECTIONS: Update section control (on/off based on position)
                var sectionsState = await _sectionService.UpdateAsync(vehicleState);

                // 5. AUTOSTEER: Send steer commands to hardware
                if (guidanceState.IsAutoSteerActive)
                {
                    await _autoSteerService.SendSteerCommandAsync(
                        guidanceState.SteerAngle,
                        vehicleState.Speed);
                }

                // 6. FIELD: Update field coverage (every 10 ticks = 1 second at 10 Hz)
                if (_tickCounter % 10 == 0)
                {
                    await _fieldService.UpdateCoverageAsync(vehicleState.Position);
                }

                // ============================================================
                // BUILD COMPLETE STATE DTO
                // ============================================================

                var completeState = new ApplicationStateDto
                {
                    Vehicle = vehicleState.ToDto(),
                    Guidance = guidanceState.ToDto(),
                    Sections = sectionsState.ToDto(),
                    Field = _fieldService.GetCurrentFieldState().ToDto(),
                    Timestamp = DateTime.UtcNow
                };

                // ============================================================
                // BROADCAST STATE TO ALL FRONTENDS (SignalR)
                // ============================================================

                await _hubContext.Clients.All.SendAsync("StateUpdated", completeState);

                // Performance measurement
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (elapsed > _config.MainLoopIntervalMs * 0.9)
                {
                    _logger.LogWarning(
                        "Main loop tick #{Tick} took {ElapsedMs:F1} ms (target: {TargetMs} ms)",
                        _tickCounter, elapsed, _config.MainLoopIntervalMs);
                }

                // Periodic stats
                if (_tickCounter % 100 == 0)
                {
                    _logger.LogInformation(
                        "Main loop stats: Tick #{Tick}, Elapsed: {ElapsedMs:F1} ms, " +
                        "Vehicle: {Lat:F7}, {Lon:F7}, Speed: {Speed:F1} km/h",
                        _tickCounter,
                        elapsed,
                        vehicleState.Position.Latitude,
                        vehicleState.Position.Longitude,
                        vehicleState.Speed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in main loop tick #{Tick}", _tickCounter);
            }
        }

        public void Dispose()
        {
            _mainLoopTimer?.Dispose();
        }
    }
}
```

---

## Konfiguracja

### OrchestrationConfig.cs

```csharp
// AgOpenGPS.Api/Orchestration/OrchestrationConfig.cs

namespace AgOpenGPS.Api.Orchestration
{
    public class OrchestrationConfig
    {
        /// <summary>
        /// Main loop frequency in Hz (cycles per second)
        /// Default: 10 Hz (100ms interval)
        /// </summary>
        public int MainLoopFrequencyHz { get; set; } = 10;

        /// <summary>
        /// Main loop interval in milliseconds
        /// Calculated from MainLoopFrequencyHz
        /// </summary>
        public int MainLoopIntervalMs => 1000 / MainLoopFrequencyHz;

        /// <summary>
        /// Field coverage update frequency (every N ticks)
        /// Default: 10 ticks = 1 second at 10 Hz
        /// </summary>
        public int FieldCoverageUpdateInterval { get; set; } = 10;

        /// <summary>
        /// SignalR broadcast throttling (minimum interval between broadcasts)
        /// Default: 0 (no throttling, broadcast every tick)
        /// </summary>
        public int SignalRBroadcastThrottleMs { get; set; } = 0;

        /// <summary>
        /// Enable performance logging
        /// </summary>
        public bool EnablePerformanceLogging { get; set; } = true;
    }
}
```

### appsettings.json

```json
{
  "Orchestration": {
    "MainLoopFrequencyHz": 10,
    "FieldCoverageUpdateInterval": 10,
    "SignalRBroadcastThrottleMs": 0,
    "EnablePerformanceLogging": true
  }
}
```

### Dependency Injection

```csharp
// AgOpenGPS.Api/Program.cs

var builder = WebApplication.CreateBuilder(args);

// Load orchestration config
var orchestrationConfig = builder.Configuration
    .GetSection("Orchestration")
    .Get<OrchestrationConfig>() ?? new OrchestrationConfig();

builder.Services.AddSingleton(orchestrationConfig);

// Add ApplicationOrchestrator as hosted service
builder.Services.AddHostedService<ApplicationOrchestrator>();

// ... rest of DI setup
```

---

## Kolejność wykonywania (Order of Execution)

**WAŻNE:** Kolejność wywołań services **ma znaczenie**!

```
1. GnssService.GetLatestPositionAsync()
   │
   │ Returns: GnssPosition (lat, lon, altitude, heading, speed)
   │
   ▼
2. VehicleService.UpdateStateAsync(gnssPosition)
   │
   │ Calculates: Vehicle position, heading, roll, pitch
   │ Uses: IMU data, antenna offset, etc.
   │
   ▼
3. GuidanceService.UpdateAsync(vehicleState)
   │
   │ Calculates: Distance from AB line, steer angle
   │ Uses: Active AB line, vehicle position
   │
   ▼
4. SectionControlService.UpdateAsync(vehicleState)
   │
   │ Calculates: Which sections should be on/off
   │ Uses: Vehicle position, boundaries, coverage map
   │
   ▼
5. AutoSteerService.SendSteerCommandAsync(steerAngle, speed)
   │
   │ Sends: UDP packet to AutoSteer board
   │ Only if: IsAutoSteerActive == true
   │
   ▼
6. FieldService.UpdateCoverageAsync(position)
   │
   │ Updates: Coverage map (which areas have been covered)
   │ Frequency: Every 10 ticks (1 second at 10 Hz)
   │
   ▼
7. StateHub.BroadcastState(completeState)
   │
   │ SignalR: Sends ApplicationStateDto to all clients
   │ Frequency: Every tick (10 Hz)
```

---

## Timing i Performance

### Frequency Considerations

| Frequency | Interval | Use Case |
|-----------|----------|----------|
| 4 Hz      | 250ms    | **Current (FormGPS)** - może być za wolno |
| 10 Hz     | 100ms    | **Recommended** - dobry balans |
| 20 Hz     | 50ms     | High precision, może być overkill |
| 30 Hz     | 33ms     | Max dla GNSS (większość 10 Hz) |

**Rekomendacja:** 10 Hz (100ms) - szybciej niż obecne 4 Hz, ale nie za szybko.

### Performance Budget

**Target:** Każdy tick powinien zakończyć się w < 90ms (90% of 100ms interval)

**Breakdown:**

| Operation | Target Time | Notes |
|-----------|-------------|-------|
| GNSS read | < 5ms | Serial/UDP read from buffer |
| Vehicle update | < 10ms | Position calculations |
| Guidance update | < 20ms | AB line distance, steer angle |
| Section control | < 15ms | Boundary checks, coverage map |
| AutoSteer send | < 5ms | UDP send (non-blocking) |
| Field coverage | < 10ms | Only every 10 ticks |
| State DTO build | < 5ms | Object creation, ToDto() |
| SignalR broadcast | < 20ms | In-process: fast, HTTP: slower |
| **TOTAL** | **< 90ms** | **Leaves 10ms safety margin** |

### Monitoring

```csharp
private async void MainLoopTick(object state)
{
    var sw = Stopwatch.StartNew();

    // ... main loop operations ...

    sw.Stop();

    if (sw.ElapsedMilliseconds > 90)
    {
        _logger.LogWarning("Main loop tick too slow: {Ms} ms", sw.ElapsedMilliseconds);
    }

    // Telemetry
    _telemetry.RecordMainLoopDuration(sw.ElapsedMilliseconds);
}
```

---

## Migracja z FormGPS Timer

### Krok 1: Identyfikacja logiki

**Current FormGPS.tmrWatchdog_Tick():**

```csharp
private void tmrWatchdog_Tick(object sender, EventArgs e)
{
    // BUSINESS LOGIC (PRZENIESIONE DO BACKENDU)
    pn.UpdatePosition();                    → VehicleService.UpdateStateAsync()
    guidanceLine.UpdateGuidanceLine();      → GuidanceService.UpdateAsync()
    section.UpdateSections();               → SectionControlService.UpdateAsync()
    mc.SendAutoSteerData();                 → AutoSteerService.SendSteerCommandAsync()

    // UI UPDATES (POZOSTAJĄ W FORMGPS, ALE TRIGGERED BY SIGNALR)
    lblSpeed.Text = ...;                    → OnStateUpdated(state)
    lblHeading.Text = ...;                  → OnStateUpdated(state)
    oglMain.Invalidate();                   → OnStateUpdated(state)
}
```

### Krok 2: Ekstrakcja do Services

**Przed:**
```csharp
// FormGPS.cs
private void tmrWatchdog_Tick(object sender, EventArgs e)
{
    // Direct access to fields
    double distance = guidanceLine.distanceFromCurrentLine;
    double steerAngle = guidanceLine.steerAngleGu;

    lblDistance.Text = distance.ToString("F2");
}
```

**Po:**
```csharp
// Backend: GuidanceService.cs
public async Task<GuidanceState> UpdateAsync(VehicleState vehicle)
{
    var distance = _abLine.CalculateDistance(vehicle.Position);
    var steerAngle = CalculateSteerAngle(distance, vehicle.Speed);

    return new GuidanceState
    {
        DistanceFromLine = distance,
        SteerAngle = steerAngle
    };
}

// Frontend: FormGPS.cs (SignalR handler)
private void OnStateUpdated(ApplicationStateDto state)
{
    _uiContext.Post(_ =>
    {
        lblDistance.Text = state.Guidance.DistanceFromLine.ToString("F2");
    }, null);
}
```

### Krok 3: Usunięcie timera z FormGPS

```csharp
// FormGPS.cs

// DELETE THIS:
// private void tmrWatchdog_Tick(object sender, EventArgs e) { ... }

// REPLACE WITH SignalR handler:
private void OnStateUpdated(ApplicationStateDto state)
{
    // UI updates only (no business logic!)
    UpdateVehicleDisplay(state.Vehicle);
    UpdateGuidanceDisplay(state.Guidance);
    UpdateSectionsDisplay(state.Sections);

    oglMain.Invalidate();
}
```

---

## Testing

### Unit Tests

```csharp
// AgOpenGPS.Api.Tests/Orchestration/ApplicationOrchestratorTests.cs

[TestFixture]
public class ApplicationOrchestratorTests
{
    [Test]
    public async Task MainLoop_CallsServicesInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<string>();

        var gnssService = Substitute.For<IGnssService>();
        gnssService.GetLatestPositionAsync()
            .Returns(Task.FromResult(new GnssPosition()))
            .AndDoes(_ => callOrder.Add("GNSS"));

        var vehicleService = Substitute.For<IVehicleService>();
        vehicleService.UpdateStateAsync(Arg.Any<GnssPosition>())
            .Returns(Task.FromResult(new VehicleState()))
            .AndDoes(_ => callOrder.Add("Vehicle"));

        var guidanceService = Substitute.For<IGuidanceService>();
        guidanceService.UpdateAsync(Arg.Any<VehicleState>())
            .Returns(Task.FromResult(new GuidanceState()))
            .AndDoes(_ => callOrder.Add("Guidance"));

        var orchestrator = new ApplicationOrchestrator(
            /* ... inject mocks ... */);

        // Act
        await orchestrator.StartAsync(CancellationToken.None);
        await Task.Delay(150); // Wait for > 1 tick at 10 Hz

        // Assert
        Assert.That(callOrder, Is.EqualTo(new[] { "GNSS", "Vehicle", "Guidance" }));
    }

    [Test]
    public async Task MainLoop_BroadcastsStateViaSignalR()
    {
        // Arrange
        var hubContext = Substitute.For<IHubContext<StateHub>>();
        ApplicationStateDto broadcastedState = null;

        hubContext.Clients.All.SendAsync("StateUpdated", Arg.Any<ApplicationStateDto>())
            .Returns(Task.CompletedTask)
            .AndDoes(call => broadcastedState = call.Arg<ApplicationStateDto>());

        var orchestrator = new ApplicationOrchestrator(hubContext, /* ... */);

        // Act
        await orchestrator.StartAsync(CancellationToken.None);
        await Task.Delay(150);

        // Assert
        Assert.That(broadcastedState, Is.Not.Null);
        Assert.That(broadcastedState.Vehicle, Is.Not.Null);
        Assert.That(broadcastedState.Guidance, Is.Not.Null);
    }
}
```

### Integration Tests

```csharp
// Test całego main loop z prawdziwymi services (bez hardware)

[TestFixture]
public class MainLoopIntegrationTests
{
    [Test]
    public async Task MainLoop_ProcessesGnssData_UpdatesGuidance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IGnssService, MockGnssService>(); // Mock hardware
        services.AddSingleton<IVehicleService, VehicleService>(); // Real
        services.AddSingleton<IGuidanceService, GuidanceService>(); // Real
        // ... etc

        var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<ApplicationOrchestrator>();

        // Act
        await orchestrator.StartAsync(CancellationToken.None);
        await Task.Delay(1000); // 10 ticks at 10 Hz
        await orchestrator.StopAsync(CancellationToken.None);

        // Assert
        var guidanceService = provider.GetRequiredService<IGuidanceService>();
        var state = guidanceService.GetCurrentState();

        Assert.That(state.DistanceFromLine, Is.GreaterThan(0));
    }
}
```

---

## Optimization Strategies

### 1. State Change Detection

Broadcast tylko gdy state się zmienił:

```csharp
private ApplicationStateDto _lastBroadcastedState;

private async void MainLoopTick(object state)
{
    var newState = BuildCompleteState();

    // Only broadcast if state changed
    if (!newState.Equals(_lastBroadcastedState))
    {
        await _hubContext.Clients.All.SendAsync("StateUpdated", newState);
        _lastBroadcastedState = newState;
    }
}
```

### 2. Partial Updates

Zamiast broadcast całego state, broadcast tylko co się zmieniło:

```csharp
// Instead of ApplicationStateDto, use:
public class StateUpdateDto
{
    public VehicleStateDto Vehicle { get; set; }    // Changed
    public GuidanceStateDto Guidance { get; set; }  // Changed
    public SectionStateDto[] Sections { get; set; } // null if not changed
}
```

### 3. Throttling

Ograniczenie frequency dla niektórych updates:

```csharp
private int _tickCounter = 0;

// Vehicle/Guidance: Every tick (10 Hz)
await BroadcastVehicleAndGuidance();

// Sections: Every 2 ticks (5 Hz)
if (_tickCounter % 2 == 0)
{
    await BroadcastSections();
}

// Field coverage: Every 10 ticks (1 Hz)
if (_tickCounter % 10 == 0)
{
    await BroadcastFieldCoverage();
}
```

---

## Failure Handling

### Service Errors

```csharp
private async void MainLoopTick(object state)
{
    try
    {
        var gnssData = await _gnssService.GetLatestPositionAsync();
    }
    catch (GnssTimeoutException ex)
    {
        _logger.LogWarning("GNSS timeout, using last known position");

        // Use cached position
        var vehicleState = _vehicleService.GetLastKnownState();

        // Continue with degraded functionality
        // ...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Critical error in main loop");

        // Broadcast error state to frontend
        await _hubContext.Clients.All.SendAsync("Error", new ErrorDto
        {
            Message = "Main loop error",
            Severity = ErrorSeverity.Critical
        });
    }
}
```

### Timer Overrun

```csharp
private bool _isTickInProgress = false;

private async void MainLoopTick(object state)
{
    if (_isTickInProgress)
    {
        _logger.LogWarning("Main loop overrun detected! Previous tick still running.");
        return; // Skip this tick
    }

    _isTickInProgress = true;

    try
    {
        // ... main loop operations ...
    }
    finally
    {
        _isTickInProgress = false;
    }
}
```

---

## Podsumowanie

### ApplicationOrchestrator vs FormGPS Timer

| Aspect | FormGPS Timer (old) | ApplicationOrchestrator (new) |
|--------|---------------------|-------------------------------|
| **Ownership** | Frontend | Backend |
| **Frequency** | 4 Hz (250ms) | 10 Hz (100ms) |
| **Coupling** | Tight (UI + logic) | Loose (services) |
| **Testability** | Hard (needs FormGPS) | Easy (unit tests) |
| **Portability** | WinForms only | Any frontend (WinForms, Electron, mobile) |
| **Performance** | Mixed with UI thread | Dedicated background thread |
| **Monitoring** | None | Logging, telemetry, metrics |

### Key Benefits

1. ✅ **Separation of Concerns:** Business logic oddzielona od UI
2. ✅ **Backend Control:** Backend steruje aplikacją, nie frontend
3. ✅ **Testability:** Łatwe unit testy i integration testy
4. ✅ **Performance:** Można zwiększyć frequency bez wpływu na UI
5. ✅ **Multi-Frontend:** Jeden backend, wiele frontendów (WinForms, Electron, etc.)
6. ✅ **Monitoring:** Pełna visibility do performance i timing

---

## Dokumenty powiązane

- [phase1-api-architecture.md](phase1-api-architecture.md) - Ogólna architektura API
- [phase1-backend-communication.md](phase1-backend-communication.md) - SignalR communication
- [phase1-migration-strategy.md](phase1-migration-strategy.md) - Strategia migracji
- [phase1-milestones.md](phase1-milestones.md) - Roadmap
