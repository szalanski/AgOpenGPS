# Task 3: Create ApplicationOrchestrator

## Goal

Create the ApplicationOrchestrator class in the backend that runs the main application loop at 10 Hz (100ms intervals), generates strongly-typed ApplicationState updates, and broadcasts them using IStatePublisher abstraction.

## Steps

1. Add project reference to AgOpenGPS.Api.Client (for ApplicationState model)
2. Create ApplicationOrchestrator class in AgOpenGPS.Api/Services/
3. Inject ILogger for diagnostics
4. Inject IStatePublisher (transport abstraction)
5. Add System.Threading.Timer for 10 Hz execution (100ms interval)
6. Create OnTick method that executes every 100ms
7. Generate strongly-typed ApplicationState (only Timestamp)
8. Call IStatePublisher.BroadcastStateAsync(state) on each tick
9. Add Start and Stop methods
10. Implement IHostedService for automatic startup with API
11. Verify timer runs at correct interval

## Key Points

- Use System.Threading.Timer (not System.Timers.Timer - more reliable)
- Timer interval: 100ms (10 Hz frequency)
- **Depends on IStatePublisher** (not concrete SignalR implementation)
- **Generates ApplicationState** (strongly-typed, only Timestamp for now)
- Runs as hosted service (starts automatically when API starts)
- Logging on each tick (helps verify timing)
- No direct dependency on SignalR or StateHub (clean separation)

## State Generation

On each tick (every 100ms):

1. Create new ApplicationState instance
2. Set Timestamp = DateTime.UtcNow
3. Call `await _statePublisher.BroadcastStateAsync(state)`
4. Log tick execution

**ApplicationState structure** (from AgOpenGPS.Api.Client):
```csharp
public class ApplicationState
{
    public DateTime Timestamp { get; set; }
}
```

## Timing Precision

- Target: 100ms intervals (10 Hz)
- Acceptable drift: +/- 5ms
- Use high-resolution timer if available
- Log actual tick duration for diagnostics

## Dependency Injection

Constructor should inject:
- `ILogger<ApplicationOrchestrator>` - for logging
- `IStatePublisher` - for broadcasting state (abstraction, not SignalR directly)

Register in Program.cs:
```csharp
builder.Services.AddHostedService<ApplicationOrchestrator>();
// IStatePublisher registration happens in Task 4
```

## Acceptance

- [ ] Project reference to AgOpenGPS.Api.Client added
- [ ] ApplicationOrchestrator class exists in AgOpenGPS.Api/Services/
- [ ] Implements IHostedService
- [ ] Injects IStatePublisher (not IHubContext or SignalR types)
- [ ] Timer runs at 100ms intervals
- [ ] OnTick method executes every tick
- [ ] Generates strongly-typed ApplicationState with Timestamp
- [ ] Calls IStatePublisher.BroadcastStateAsync(state) on each tick
- [ ] Logging shows tick execution
- [ ] Starts automatically when API starts
- [ ] No direct dependency on SignalR (only IStatePublisher abstraction)

## Test

Run AgOpenGPS.Api - logs should show ticks executing every 100ms with UTC timestamp.
Note: Broadcasting won't work yet (IStatePublisher not registered) - Task 4 will implement and register SignalRStatePublisher.
