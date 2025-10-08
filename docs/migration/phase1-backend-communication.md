# Faza 1: Backend-Driven Communication Architecture

**Data utworzenia:** 2025-10-08
**Status:** Zaakceptowany
**Decyzja:** SignalR dla komunikacji Backend → Frontend

---

## Przegląd

W architekturze Backend-driven, **backend steruje całą aplikacją** poprzez główną pętlę (main loop), a frontend jedynie:
1. **Wyświetla zmiany** otrzymane z backendu
2. **Inicjuje akcje użytkownika** (click → API call)

To odwrotność Frontend-driven, gdzie frontend ma timer i odpytuje backend o dane.

---

## Porównanie Frontend-driven vs Backend-driven

### ❌ Frontend-driven (BŁĘDNE podejście)

```
┌─────────────────────────────────────┐
│           Frontend (WinForms)       │
│                                     │
│  Timer (250ms) ──────┐              │
│                      ↓              │
│  API.GetState() ────────────┐       │
└─────────────────────────────┼───────┘
                              │
                              │ HTTP GET
                              ↓
┌─────────────────────────────────────┐
│            Backend (API)            │
│                                     │
│  Calculate() → Return state         │
└─────────────────────────────────────┘
```

**Problemy:**
- ❌ Frontend kontroluje timing (250ms interval)
- ❌ Polling overhead (niepotrzebne HTTP calls)
- ❌ Backend jest pasywny (tylko odpowiada)
- ❌ Trudno zsynchronizować timing między frontendem a backendem

---

### ✅ Backend-driven (POPRAWNE podejście)

```
┌─────────────────────────────────────┐
│            Backend (API)            │
│                                     │
│  ApplicationOrchestrator            │
│    Main Loop (10 Hz = 100ms)        │
│         ↓                           │
│    1. Read hardware (GNSS, IMU)     │
│    2. Update vehicle state          │
│    3. Calculate guidance            │
│    4. Update sections               │
│         ↓                           │
│    SignalR Hub.BroadcastState()     │
└──────────────┬──────────────────────┘
               │
               │ SignalR Push
               ↓
┌─────────────────────────────────────┐
│           Frontend (WinForms)       │
│                                     │
│  HubConnection.On("StateUpdated")   │
│         ↓                           │
│  Invoke(UI thread) → Update display │
└─────────────────────────────────────┘
```

**Korzyści:**
- ✅ Backend kontroluje timing (100ms main loop)
- ✅ Push model (brak polling overhead)
- ✅ Backend jest aktywny (orchestrates całą logiką)
- ✅ Frontend jest cienki (tylko wyświetla)
- ✅ Łatwa migracja Phase 1 (in-process) → Phase 2 (HTTP)

---

## SignalR dla .NET Framework 4.8

### Dlaczego SignalR?

1. **Kompatybilność:** `Microsoft.AspNetCore.SignalR.Client` wspiera .NET Framework 4.6.2+ (w tym 4.8)
2. **Real-time push:** Backend może wysyłać dane do frontendu w dowolnym momencie
3. **Łatwa migracja:**
   - Faza 1: In-process SignalR (brak HTTP overhead)
   - Faza 2: HTTP SignalR (tylko zmiana URL connection)
4. **Thread-safe UI updates:** Wspiera `SynchronizationContext` dla WinForms

### NuGet Packages

**Backend (.NET 8):**
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
```

**Frontend (.NET Framework 4.8):**
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="9.0.0" />
```

---

## Architektura Backend

### 1. ApplicationOrchestrator (Main Loop)

**Odpowiedzialność:**
- Główna pętla 10 Hz (100ms interval)
- Koordynacja wszystkich services
- Publikowanie state updates przez SignalR

**Kod:**

```csharp
// AgOpenGPS.Api/Services/ApplicationOrchestrator.cs

using Microsoft.AspNetCore.SignalR;

public class ApplicationOrchestrator : IHostedService
{
    private readonly IHubContext<StateHub> _hubContext;
    private readonly IGnssService _gnssService;
    private readonly IVehicleService _vehicleService;
    private readonly IGuidanceService _guidanceService;
    private readonly ISectionControlService _sectionService;
    private readonly ILogger<ApplicationOrchestrator> _logger;

    private Timer _mainLoopTimer;
    private const int MainLoopIntervalMs = 100; // 10 Hz

    public ApplicationOrchestrator(
        IHubContext<StateHub> hubContext,
        IGnssService gnssService,
        IVehicleService vehicleService,
        IGuidanceService guidanceService,
        ISectionControlService sectionService,
        ILogger<ApplicationOrchestrator> logger)
    {
        _hubContext = hubContext;
        _gnssService = gnssService;
        _vehicleService = vehicleService;
        _guidanceService = guidanceService;
        _sectionService = sectionService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ApplicationOrchestrator starting main loop at {Frequency} Hz",
            1000.0 / MainLoopIntervalMs);

        _mainLoopTimer = new Timer(
            MainLoopTick,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(MainLoopIntervalMs));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ApplicationOrchestrator stopping");
        _mainLoopTimer?.Dispose();
        return Task.CompletedTask;
    }

    private async void MainLoopTick(object state)
    {
        try
        {
            // 1. Read hardware data (GNSS, IMU, etc.)
            var gnssData = await _gnssService.GetLatestPositionAsync();

            // 2. Update vehicle state
            var vehicleState = await _vehicleService.UpdateStateAsync(gnssData);

            // 3. Calculate guidance
            var guidanceState = await _guidanceService.UpdateAsync(vehicleState);

            // 4. Update section control
            var sectionsState = await _sectionService.UpdateAsync(vehicleState);

            // 5. Build complete state DTO
            var completeState = new ApplicationStateDto
            {
                Vehicle = vehicleState.ToDto(),
                Guidance = guidanceState.ToDto(),
                Sections = sectionsState.ToDto(),
                Timestamp = DateTime.UtcNow
            };

            // 6. Broadcast to all connected clients via SignalR
            await _hubContext.Clients.All.SendAsync("StateUpdated", completeState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in main loop tick");
        }
    }
}
```

### 2. SignalR Hub

**Odpowiedzialność:**
- Endpoint dla SignalR connections
- Broadcast state updates (Backend → Frontend)
- Receive user actions (Frontend → Backend)
- Handle client connections/disconnections

**Kod:**

```csharp
// AgOpenGPS.Api/Hubs/StateHub.cs

using Microsoft.AspNetCore.SignalR;

public class StateHub : Hub
{
    private readonly IGuidanceService _guidanceService;
    private readonly IFieldService _fieldService;
    private readonly ISectionControlService _sectionService;
    private readonly ILogger<StateHub> _logger;

    public StateHub(
        IGuidanceService guidanceService,
        IFieldService fieldService,
        ISectionControlService sectionService,
        ILogger<StateHub> logger)
    {
        _guidanceService = guidanceService;
        _fieldService = fieldService;
        _sectionService = sectionService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ============================================================
    // USER ACTIONS (Frontend → Backend)
    // ============================================================

    /// <summary>
    /// Client wywołuje tę metodę aby utworzyć AB Line
    /// </summary>
    public async Task CreateABLine(CreateABLineDto dto)
    {
        _logger.LogInformation("Client {ConnectionId} requested AB line creation", Context.ConnectionId);

        await _guidanceService.CreateABLineAsync(dto.PointA, dto.PointB);

        // State update będzie wysłany w kolejnym main loop tick
    }

    /// <summary>
    /// Client wywołuje tę metodę aby usunąć AB Line
    /// </summary>
    public async Task DeleteABLine()
    {
        _logger.LogInformation("Client {ConnectionId} requested AB line deletion", Context.ConnectionId);

        await _guidanceService.DeleteABLineAsync();
    }

    /// <summary>
    /// Client wywołuje tę metodę aby toggle section on/off
    /// </summary>
    public async Task ToggleSection(int sectionIndex)
    {
        _logger.LogInformation("Client {ConnectionId} toggled section {Index}", Context.ConnectionId, sectionIndex);

        await _sectionService.ToggleSectionAsync(sectionIndex);
    }

    /// <summary>
    /// Client wywołuje tę metodę aby załadować field
    /// </summary>
    public async Task LoadField(string fieldName)
    {
        _logger.LogInformation("Client {ConnectionId} loading field: {FieldName}", Context.ConnectionId, fieldName);

        await _fieldService.LoadFieldAsync(fieldName);
    }

    /// <summary>
    /// Client wywołuje tę metodę aby save current field
    /// </summary>
    public async Task SaveField()
    {
        _logger.LogInformation("Client {ConnectionId} saving field", Context.ConnectionId);

        await _fieldService.SaveCurrentFieldAsync();
    }
}
```

### 3. Dependency Injection Setup

```csharp
// AgOpenGPS.Api/Program.cs

var builder = WebApplication.CreateBuilder(args);

// Add SignalR
builder.Services.AddSignalR();

// Add ApplicationOrchestrator as hosted service
builder.Services.AddHostedService<ApplicationOrchestrator>();

// Add business services
builder.Services.AddSingleton<IGnssService, GnssService>();
builder.Services.AddSingleton<IVehicleService, VehicleService>();
builder.Services.AddSingleton<IGuidanceService, GuidanceService>();
builder.Services.AddSingleton<ISectionControlService, SectionControlService>();

var app = builder.Build();

// Map SignalR hub
app.MapHub<StateHub>("/hubs/state");

app.Run();
```

---

## Architektura Frontend (WinForms)

### 1. SignalR Client Setup

**Odpowiedzialność:**
- Połączenie z SignalR Hub
- Odbieranie state updates
- Thread-safe updates UI

**Kod:**

```csharp
// GPS/Forms/FormGPS.cs

using Microsoft.AspNetCore.SignalR.Client;
using System.Threading;
using System.Windows.Forms;

public partial class FormGPS : Form
{
    private HubConnection _hubConnection;
    private readonly SynchronizationContext _uiContext;

    public FormGPS()
    {
        InitializeComponent();

        // Capture UI synchronization context
        _uiContext = SynchronizationContext.Current;

        InitializeSignalRConnection();
    }

    private void InitializeSignalRConnection()
    {
        // Create connection to SignalR hub
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/hubs/state") // Phase 2: zmień na HTTP URL
            .WithAutomaticReconnect()
            .Build();

        // Register handler for state updates
        _hubConnection.On<ApplicationStateDto>("StateUpdated", OnStateUpdated);

        // Handle connection events
        _hubConnection.Closed += async (error) =>
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await _hubConnection.StartAsync();
        };
    }

    private async void FormGPS_Load(object sender, EventArgs e)
    {
        try
        {
            // Start SignalR connection
            await _hubConnection.StartAsync();
            lblConnectionStatus.Text = "Connected";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to connect: {ex.Message}");
        }
    }

    private void OnStateUpdated(ApplicationStateDto state)
    {
        // CRITICAL: Marshal to UI thread
        _uiContext.Post(_ =>
        {
            // Update UI controls
            UpdateVehicleDisplay(state.Vehicle);
            UpdateGuidanceDisplay(state.Guidance);
            UpdateSectionsDisplay(state.Sections);

            // Trigger OpenGL redraw
            oglMain.Invalidate();
        }, null);
    }

    private void UpdateVehicleDisplay(VehicleStateDto vehicle)
    {
        lblSpeed.Text = $"{vehicle.Speed:F1} km/h";
        lblHeading.Text = $"{vehicle.Heading:F1}°";
        lblLatitude.Text = vehicle.Position.Latitude.ToString("F7");
        lblLongitude.Text = vehicle.Position.Longitude.ToString("F7");
    }

    private void UpdateGuidanceDisplay(GuidanceStateDto guidance)
    {
        lblDistanceFromLine.Text = $"{guidance.DistanceFromLine:F2} m";
        lblSteerAngle.Text = $"{guidance.SteerAngle:F1}°";

        // Update OpenGL data (thread-safe)
        _currentGuidanceState = guidance;
    }

    private void UpdateSectionsDisplay(SectionStateDto[] sections)
    {
        for (int i = 0; i < sections.Length; i++)
        {
            var section = sections[i];
            var button = _sectionButtons[i];
            button.BackColor = section.IsOn ? Color.Green : Color.Red;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Cleanup SignalR connection
        _hubConnection?.StopAsync().Wait();
        _hubConnection?.DisposeAsync().AsTask().Wait();

        base.OnFormClosing(e);
    }
}
```

### 2. User Actions (Frontend → Backend)

**Rekomendacja: SignalR Hub Method Call**

Używamy **SignalR Hub methods** dla wszystkich user actions, ponieważ połączenie SignalR jest już otwarte.

```csharp
// GPS/Forms/FormGPS.cs

private async void btnCreateABLine_Click(object sender, EventArgs e)
{
    try
    {
        var dto = new CreateABLineDto
        {
            PointA = _currentVehiclePosition,
            PointB = CalculatePointB(_currentVehiclePosition, _currentHeading)
        };

        // Invoke server method via SignalR (przez istniejące połączenie)
        await _hubConnection.InvokeAsync("CreateABLine", dto);

        // Backend response będzie w kolejnym StateUpdated broadcast
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Failed to create AB line: {ex.Message}");
    }
}
```

**Korzyści SignalR dla user actions:**
- ✅ Brak overhead tworzenia nowych HTTP connections
- ✅ Bidirectional communication przez jedno połączenie
- ✅ Natychmiastowy response przez StateUpdated broadcast
- ✅ Spójny mechanizm (SignalR dla wszystkiego)

**REST API endpoints** - opcjonalne, dla:

Możemy zachować REST API endpoints dla external integrations lub debugging:

```csharp
// AgOpenGPS.Api/Controllers/GuidanceController.cs

[ApiController]
[Route("api/guidance")]
public class GuidanceController : ControllerBase
{
    private readonly IGuidanceService _guidanceService;

    [HttpPost("abline/create")]
    public async Task<IActionResult> CreateABLine([FromBody] CreateABLineDto dto)
    {
        await _guidanceService.CreateABLineAsync(dto.PointA, dto.PointB);
        return Ok();
    }
}
```

**Użycie REST API:**
- External integrations (inne aplikacje wywołujące AgOpenGPS API)
- Debugging (Swagger UI, Postman)
- Testing (curl, integration tests)

---

## In-Process SignalR (Faza 1)

Aby uniknąć HTTP overhead w Fazie 1, możemy użyć **in-process SignalR**.

### Implementacja

```csharp
// AgOpenGPS.Api.Client/InProcess/InProcessSignalRConnection.cs

public class InProcessSignalRConnection : IHubConnection
{
    private readonly IHubContext<StateHub> _hubContext;
    private readonly Dictionary<string, Delegate> _handlers = new();

    public InProcessSignalRConnection(IServiceProvider serviceProvider)
    {
        _hubContext = serviceProvider.GetRequiredService<IHubContext<StateHub>>();

        // Hook into hub's broadcast mechanism (simplified)
        SubscribeToHubEvents();
    }

    public void On<T>(string methodName, Action<T> handler)
    {
        _handlers[methodName] = handler;
    }

    private void SubscribeToHubEvents()
    {
        // In-process: listen directly to backend events
        // (Implementation depends on EventBus or similar)
    }

    public Task InvokeAsync(string methodName, params object[] args)
    {
        // In-process: call service directly
        // Similar to InProcessApiClient
        return Task.CompletedTask;
    }
}
```

### Frontend Setup (in-process)

```csharp
// GPS/Program.cs

static void Main()
{
    // Create backend services (in-process)
    var backendServices = new ServiceCollection();
    // ... add all backend services
    var backendProvider = backendServices.BuildServiceProvider();

    // Create in-process SignalR connection
    var hubConnection = new InProcessSignalRConnection(backendProvider);

    // Start ApplicationOrchestrator
    var orchestrator = backendProvider.GetRequiredService<IHostedService>() as ApplicationOrchestrator;
    orchestrator.StartAsync(CancellationToken.None).Wait();

    // Run WinForms
    Application.Run(new FormGPS(hubConnection));
}
```

**Korzyść:** Zero HTTP overhead, wszystko in-process, szybkie debugging.

---

## Migracja Phase 1 → Phase 2

### Phase 1 (In-Process)

```csharp
// Frontend
var hubConnection = new InProcessSignalRConnection(backendProvider);
```

### Phase 2 (HTTP)

```csharp
// Frontend - TYLKO zmiana tego:
var hubConnection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/state")
    .WithAutomaticReconnect()
    .Build();
```

**Reszta kodu pozostaje bez zmian!**

---

## Thread Safety i UI Updates

### Problem

SignalR callbacks wykonują się na background thread, ale WinForms wymaga UI updates na UI thread.

### Rozwiązanie: SynchronizationContext

```csharp
private readonly SynchronizationContext _uiContext;

public FormGPS()
{
    InitializeComponent();

    // Capture UI thread context
    _uiContext = SynchronizationContext.Current;
}

private void OnStateUpdated(ApplicationStateDto state)
{
    // Marshal to UI thread
    _uiContext.Post(_ =>
    {
        // Safe to update UI controls here
        lblSpeed.Text = state.Vehicle.Speed.ToString();
    }, null);
}
```

### Alternatywa: Control.Invoke()

```csharp
private void OnStateUpdated(ApplicationStateDto state)
{
    if (InvokeRequired)
    {
        Invoke(new Action(() => OnStateUpdated(state)));
        return;
    }

    // Safe to update UI here
    lblSpeed.Text = state.Vehicle.Speed.ToString();
}
```

---

## Data Transfer Objects (DTOs)

### ApplicationStateDto (Complete State)

```csharp
public class ApplicationStateDto
{
    public VehicleStateDto Vehicle { get; set; }
    public GuidanceStateDto Guidance { get; set; }
    public SectionStateDto[] Sections { get; set; }
    public FieldStateDto Field { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### VehicleStateDto

```csharp
public class VehicleStateDto
{
    public Vector3Dto Position { get; set; }
    public double Heading { get; set; }
    public double Speed { get; set; }
    public double RollAngle { get; set; }
    public bool IsGnssValid { get; set; }
    public int SatelliteCount { get; set; }
}
```

### GuidanceStateDto

```csharp
public class GuidanceStateDto
{
    public double DistanceFromLine { get; set; }
    public double SteerAngle { get; set; }
    public bool IsGuidanceActive { get; set; }
    public ABLineStateDto ABLine { get; set; }
}
```

---

## Timing i Performance

### Main Loop Frequency

**Current (FormGPS.tmrWatchdog):** 250ms = 4 Hz
**New (ApplicationOrchestrator):** 100ms = 10 Hz

**Dlaczego 10 Hz?**
- Guidance system wymaga szybkich reakcji
- GNSS updates przychodzą 5-10 Hz
- OpenGL rendering 30-60 FPS (niezależne)

### Optimization

Jeśli state się nie zmienił, nie broadcast:

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

---

## Podsumowanie

### Architektura Backend-Driven

✅ **Backend:**
- `ApplicationOrchestrator` - main loop (10 Hz)
- `StateHub` - SignalR hub
- Services - business logic
- State broadcast via SignalR

✅ **Frontend:**
- `HubConnection` - SignalR client
- `.On("StateUpdated")` - receive updates
- `SynchronizationContext` - thread-safe UI updates
- Thin UI - tylko wyświetlanie

✅ **Communication:**
- Backend → Frontend: SignalR push (real-time)
- Frontend → Backend: SignalR invoke lub REST API
- Phase 1: In-process (brak HTTP)
- Phase 2: HTTP (tylko zmiana URL)

### Korzyści

1. **Backend kontroluje timing** - frontend nie musi się martwić o główną pętlę
2. **Real-time updates** - brak polling overhead
3. **Łatwa migracja** - Phase 1 (in-process) → Phase 2 (HTTP)
4. **Testability** - backend można testować bez frontendu
5. **Scalability** - przyszłość: wiele frontendów (WinForms, Electron, mobile)

---

## Dokumenty powiązane

- [phase1-api-architecture.md](phase1-api-architecture.md) - Architektura API
- [phase1-backend-loop.md](phase1-backend-loop.md) - Szczegóły ApplicationOrchestrator
- [phase1-migration-strategy.md](phase1-migration-strategy.md) - Strategia migracji
- [phase1-technical-decisions-api.md](phase1-technical-decisions-api.md) - ADRs
