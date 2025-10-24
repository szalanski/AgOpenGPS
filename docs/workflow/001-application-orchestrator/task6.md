# Task 6: Migration from FormGPS Timer

## Objective

Migrate FormGPS from timer-driven to SignalR-driven, using adapter pattern for safe rollout.

## Context

FormGPS currently has tmrWatchdog (250ms) that does business logic + UI updates. Need to:
1. Keep legacy timer working (fallback)
2. Add SignalR client to FormGPS
3. Use feature flag to switch between legacy/new
4. Eventually delete legacy timer

See: [05-adapter-pattern.md](../../architecture/05-adapter-pattern.md) for migration strategy.

## What to Implement

### 1. Add SignalR Client to FormGPS

**Install NuGet:**
```
Microsoft.AspNetCore.SignalR.Client
```

**FormGPS.cs:**
- Private field: `HubConnection _hubConnection`
- Private field: `SynchronizationContext _uiContext`
- Method: `InitializeSignalRAsync()`
- Handler: `OnStateUpdated(ApplicationStateDto state)`

### 2. Connect to Backend Hub

In FormGPS constructor or Load:
```csharp
_uiContext = SynchronizationContext.Current;
await InitializeSignalRAsync();
```

Connection URL:
- In-process: "http://localhost:5000/hubs/state"
- Will change to HTTP in Phase 2

### 3. SignalR Event Handler

```csharp
private void OnStateUpdated(ApplicationStateDto state)
{
    // IMPORTANT: Must marshal to UI thread!
    _uiContext.Post(_ =>
    {
        UpdateVehicleDisplay(state.Vehicle);
        UpdateGuidanceDisplay(state.Guidance);
        UpdateSectionsDisplay(state.Sections);
        oglMain.Invalidate();
    }, null);
}
```

See: [04-signalr.md](../../architecture/04-signalr.md) for thread safety details.

### 4. Feature Flag

**config.json:**
```json
{
  "features": {
    "useBackendOrchestrator": false  // Start with false (legacy)
  }
}
```

**FormGPS logic:**
```csharp
if (_featureFlags.UseBackendOrchestrator)
{
    // NEW: SignalR-driven (tmrWatchdog disabled)
    await InitializeSignalRAsync();
}
else
{
    // LEGACY: Timer-driven (existing code)
    tmrWatchdog.Enabled = true;
}
```

### 5. Update Display Methods

Extract UI update logic from tmrWatchdog_Tick into separate methods:

- `UpdateVehicleDisplay(VehicleStateDto vehicle)`
- `UpdateGuidanceDisplay(GuidanceStateDto guidance)`
- `UpdateSectionsDisplay(SectionsStateDto sections)`

These can be called from:
- tmrWatchdog_Tick (legacy)
- OnStateUpdated (new)

## Migration Stages

### Stage 1: Legacy Only (Current)
```
FormGPS.tmrWatchdog (250ms)
  └─ Business logic + UI updates
```

Feature flag: `useBackendOrchestrator = false`

### Stage 2: Hybrid (Testing)
```
Both systems running:
  ├─ FormGPS.tmrWatchdog (250ms) - disabled by flag
  └─ ApplicationOrchestrator (100ms) - active
      └─ SignalR → FormGPS.OnStateUpdated()
```

Feature flag: `useBackendOrchestrator = true`

### Stage 3: Backend Only (Final)
```
ApplicationOrchestrator (100ms)
  └─ SignalR → FormGPS.OnStateUpdated()

DELETE:
  ✗ FormGPS.tmrWatchdog_Tick()
  ✗ Business logic from FormGPS
  ✗ Feature flag (always true)
```

## Acceptance Criteria

- ✅ SignalR client added to FormGPS
- ✅ Connects to backend hub on startup
- ✅ OnStateUpdated handler implemented
- ✅ UI thread safety handled (SynchronizationContext)
- ✅ Feature flag controls legacy vs new
- ✅ Legacy timer can be disabled
- ✅ UI updates at 10 Hz from SignalR
- ✅ No crashes or UI freezes

## Testing

### Test with Legacy (flag = false)

1. Set `useBackendOrchestrator = false`
2. Run FormGPS
3. Verify tmrWatchdog active (250ms)
4. Verify app works as before

### Test with New Backend (flag = true)

1. Start backend (ApplicationOrchestrator running)
2. Set `useBackendOrchestrator = true`
3. Run FormGPS
4. Verify SignalR connection established
5. Verify UI updates at 10 Hz (faster than before!)
6. Verify tmrWatchdog disabled

### A/B Testing

Run both configurations, compare:
- UI responsiveness
- Guidance accuracy
- Performance

If issues found: Flip flag back to false (instant rollback).

## Rollback Plan

If new backend has issues:
1. Set `useBackendOrchestrator = false`
2. Restart FormGPS
3. Legacy timer takes over
4. Fix backend issues
5. Try again later

**DO NOT delete legacy code until 100% confident.**

## When to Delete Legacy

Delete tmrWatchdog ONLY when:
1. ✅ New backend 100% working
2. ✅ Feature flag = true for 1+ weeks
3. ✅ Zero issues reported
4. ✅ Tests pass
5. ✅ Team agrees

## References

- [05-adapter-pattern.md](../../architecture/05-adapter-pattern.md) - Migration strategy
- [04-signalr.md](../../architecture/04-signalr.md) - SignalR details
- [02-strangler-fig.md](../../architecture/02-strangler-fig.md) - Strangler Fig pattern
