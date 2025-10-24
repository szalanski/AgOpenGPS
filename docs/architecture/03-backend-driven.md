# Backend-Driven Architecture

## Concept

**Backend owns timing, frontend only displays.**

- **Backend**: Has main loop, calculates all state, broadcasts updates
- **Frontend**: Passive receiver, updates UI when notified

## Frontend-Driven vs Backend-Driven

### ❌ Frontend-Driven (BAD - current state)

```
Frontend (WinForms)
  └─ Timer (250ms)
      └─ Call backend API
          └─ Backend calculates
              └─ Return state
                  └─ Frontend displays
```

**Problems:**
- Frontend controls timing
- Polling overhead
- Backend is passive
- Hard to sync timing

### ✅ Backend-Driven (GOOD - target)

```
Backend
  └─ Timer (100ms = 10 Hz)
      └─ Read hardware (GNSS)
          └─ Update vehicle state
              └─ Calculate guidance
                  └─ Broadcast state (SignalR)
                      ↓
Frontend
  └─ Receives update
      └─ Display in UI
```

**Benefits:**
- Backend controls timing
- No polling (push model)
- Backend is active
- Single source of timing

## ApplicationOrchestrator

**Main loop coordinator** in backend:

### Responsibilities
1. Own the timer (10 Hz = 100ms interval)
2. Coordinate service calls in correct order
3. Build complete state
4. Broadcast via SignalR

### Service Call Order

```
ApplicationOrchestrator (every 100ms)
  │
  ├─► 1. GnssService.GetLatestPositionAsync()
  │      └─ Read GNSS data from hardware buffer
  │
  ├─► 2. VehicleService.UpdateStateAsync(gnssData)
  │      └─ Calculate vehicle position, heading, speed
  │
  ├─► 3. GuidanceService.UpdateAsync(vehicleState)
  │      └─ Calculate distance from AB line, steer angle
  │
  ├─► 4. SectionControlService.UpdateAsync(vehicleState)
  │      └─ Calculate which sections on/off
  │
  └─► 5. StateHub.BroadcastState(completeState)
         └─ SignalR push to all frontends
```

**Order matters!** Guidance needs Vehicle, Sections need Vehicle position.

## Current vs Target

### Current (FormGPS)
```csharp
// FormGPS.cs
private void tmrWatchdog_Tick(object sender, EventArgs e)  // 250ms
{
    // Frontend controls timing!
    pn.UpdatePosition();
    guidanceLine.UpdateGuidanceLine();
    section.UpdateSections();

    // UI updates mixed with logic
    lblSpeed.Text = pn.speed.ToString();
    oglMain.Invalidate();
}
```

**Problems:**
- Timer in UI (250ms = 4 Hz)
- Business logic mixed with UI
- Cannot test without FormGPS

### Target (ApplicationOrchestrator)
```csharp
// AgOpenGPS.Api/Orchestration/ApplicationOrchestrator.cs
public class ApplicationOrchestrator : IHostedService
{
    private Timer _timer;

    public Task StartAsync(CancellationToken ct)
    {
        _timer = new Timer(MainLoopTick, null, 0, 100); // 10 Hz
        return Task.CompletedTask;
    }

    private async void MainLoopTick(object state)
    {
        // 1-5: Service calls (in order)
        var gnss = await _gnssService.GetLatestPositionAsync();
        var vehicle = await _vehicleService.UpdateStateAsync(gnss);
        var guidance = await _guidanceService.UpdateAsync(vehicle);
        var sections = await _sectionService.UpdateAsync(vehicle);

        var completeState = new ApplicationStateDto {
            Vehicle = vehicle.ToDto(),
            Guidance = guidance.ToDto(),
            Sections = sections.ToDto()
        };

        await _hubContext.Clients.All.SendAsync("StateUpdated", completeState);
    }
}
```

**Benefits:**
- Timer in backend (100ms = 10 Hz)
- Pure business logic
- Testable without UI

### Frontend (Target)
```csharp
// FormGPS.cs
private SynchronizationContext _uiContext;
private HubConnection _hubConnection;

private void InitializeSignalR()
{
    _hubConnection.On<ApplicationStateDto>("StateUpdated", OnStateUpdated);
}

private void OnStateUpdated(ApplicationStateDto state)
{
    _uiContext.Post(_ =>
    {
        // ONLY UI updates (no logic!)
        lblSpeed.Text = state.Vehicle.Speed.ToString();
        lblHeading.Text = state.Vehicle.Heading.ToString();
        oglMain.Invalidate();
    }, null);
}
```

**Benefits:**
- No timer in frontend
- UI updates only
- Clean separation

## Migration Path

**Phase A: Hybrid (both timers)**
```csharp
// FormGPS
private bool _useBackendLoop = false; // Feature Flag

private void tmrWatchdog_Tick(object sender, EventArgs e)
{
    if (_useBackendLoop)
        return; // Backend handles it
    else
        LegacyLogic(); // OLD
}
```

**Phase B: Backend only**
```csharp
// FormGPS
public FormGPS()
{
    tmrWatchdog.Enabled = false; // Disable timer
    InitializeSignalR();          // Use SignalR
}
```

**Phase C: Timer deleted**
```csharp
// tmrWatchdog removed from Designer
// Only SignalR handler remains
```

## Key Differences

| Aspect | Frontend-Driven (old) | Backend-Driven (new) |
|--------|----------------------|----------------------|
| **Timer** | FormGPS (250ms) | ApplicationOrchestrator (100ms) |
| **Control** | Frontend controls | Backend controls |
| **Logic** | Mixed in timer | Separated in services |
| **Testing** | Needs FormGPS | Testable standalone |
| **Frequency** | 4 Hz | 10 Hz (faster!) |

## References

- See: 04-signalr.md (how backend broadcasts)
- See: workflow/001-application-orchestrator/plan.md (implementation)
