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
  └─ Timer (250ms = 4 Hz)
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
1. Own the timer (4 Hz = 250ms interval)
2. Coordinate service calls in correct order
3. Build complete state
4. Broadcast via SignalR

### Service Call Order

```
ApplicationOrchestrator (every 250ms)
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
// FormGPS.cs (LEGACY - now replaced by ProcessApplicationTick)
private void tmrWatchdog_Tick(object sender, EventArgs e)  // 250ms (was tmrWatchdog timer)
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
- Timer in UI - DELETED (was 250ms = 4 Hz)
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
        _timer = new Timer(MainLoopTick, null, 0, 250); // 4 Hz
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
- Timer in backend (250ms = 4 Hz)
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
| **Timer** | FormGPS (250ms) - DELETED | ApplicationOrchestrator (250ms) |
| **Control** | Frontend controls | Backend controls |
| **Logic** | Mixed in timer | Separated in services |
| **Testing** | Needs FormGPS | Testable standalone |
| **Frequency** | 4 Hz | 4 Hz (same) |

## References

- See: 04-signalr.md (how backend broadcasts)
- See: workflow/001-backend-state-foundation/plan.md (implementation)

## Implementation Status (Workflow 001)

✅ **COMPLETED** - Backend State Foundation (250ms / 4 Hz)

### Implemented Components

**Backend (AgOpenGPS.Api - .NET 8)**:
- `ApplicationOrchestrator` - Main loop at 4 Hz (250ms interval)
- `IStatePublisher` - Transport abstraction interface
- `SignalRStatePublisher` - SignalR implementation
- `StateHub` - SignalR Hub for broadcasting

**Client (AgOpenGPS.Api.Client - .NET Standard 2.0)**:
- `ApplicationState` - Strongly-typed state model (Timestamp property)
- `ConnectionOptions` - Backend connection configuration (URL, auto-reconnect)
- `IStateSubscriber` - Transport abstraction (implements IDisposable/IAsyncDisposable)
- `SignalRStateSubscriber` - SignalR implementation with disposal
- `SubscriberFactory` - Factory for creating configured subscribers

**Frontend (FormGPS)**:
- **tmrWatchdog deleted** - Backend now drives main loop (Phase C complete!)
- `ProcessApplicationTick()` - Refactored timer logic (called on state updates)
- `OnStateReceived()` - SignalR state handler with UI thread marshaling
- `InitializeBackendConnection()` - SubscriberFactory integration

**Tests**:
- `StateReceptionTests` - Integration tests for state broadcasting (3 tests passing)

### Key Design Decisions

1. **4 Hz (250ms) instead of 10 Hz (100ms)**:
   - Matches original tmrWatchdog frequency
   - Preserves existing timing behavior
   - Easier migration path

2. **IStateSubscriber implements IDisposable/IAsyncDisposable**:
   - Encapsulates subscription lifecycle
   - FormGPS only tracks one object (_stateSubscriber)
   - Clean disposal pattern

3. **Subscribe() returns void**:
   - Simplifies API (fire-and-forget)
   - Subscriber manages internal subscription
   - No need to track separate subscription object

4. **SubscriberFactory pattern**:
   - Hides HubConnection creation complexity
   - FormGPS uses clean ConnectionOptions configuration
   - No SignalR types visible in FormGPS

5. **tmrWatchdog completely deleted**:
   - No fallback mode (full commitment to backend)
   - Backend is required for FormGPS operation
   - timerSim remains independent at 93ms

### File Paths

**Backend**:
- `SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs`
- `SourceCode/AgOpenGPS.Api/Services/SignalRStatePublisher.cs`
- `SourceCode/AgOpenGPS.Api/Hubs/StateHub.cs`
- `SourceCode/AgOpenGPS.Api/Abstractions/IStatePublisher.cs`

**Client**:
- `SourceCode/AgOpenGPS.Api.Client/Models/ApplicationState.cs`
- `SourceCode/AgOpenGPS.Api.Client/Models/ConnectionOptions.cs`
- `SourceCode/AgOpenGPS.Api.Client/Abstractions/IStateSubscriber.cs`
- `SourceCode/AgOpenGPS.Api.Client/SignalR/SignalRStateSubscriber.cs`
- `SourceCode/AgOpenGPS.Api.Client/Factories/SubscriberFactory.cs`

**Frontend**:
- `SourceCode/GPS/Forms/FormGPS.cs` - Backend connection logic
- `SourceCode/GPS/Forms/GUI.Designer.cs` - ProcessApplicationTick() method

**Tests**:
- `SourceCode/Tests/AgOpenGPS.API.IntegrationTests/StateReceptionTests.cs`

### Transport Swapping

To replace SignalR with another transport (WebSocket, gRPC, MQTT):

1. **Backend**: Create new `IStatePublisher` implementation
   ```csharp
   public class GrpcStatePublisher : IStatePublisher
   {
       public Task BroadcastStateAsync(ApplicationState state) { ... }
   }
   ```

2. **Change DI registration**:
   ```csharp
   services.AddSingleton<IStatePublisher, GrpcStatePublisher>();
   ```

3. **Client**: Create new `IStateSubscriber` implementation
   ```csharp
   public class GrpcStateSubscriber : IStateSubscriber, IDisposable, IAsyncDisposable
   {
       public void Subscribe(Action<ApplicationState> onNext, ...) { ... }
       public void Dispose() { ... }
       public ValueTask DisposeAsync() { ... }
   }
   ```

4. **Update SubscriberFactory**:
   ```csharp
   public static IStateSubscriber CreateGrpcSubscriber(ConnectionOptions options) { ... }
   ```

**No changes needed** in ApplicationOrchestrator or FormGPS!

### Next Steps (Future Workflows)

- [ ] Add GNSS service to backend (parse GPS data)
- [ ] Add Guidance service (AB lines, curves)
- [ ] Add Section Control service
- [ ] Enrich ApplicationState with real data (Vehicle, Guidance, Sections)
- [ ] Migrate more timer logic to backend services
