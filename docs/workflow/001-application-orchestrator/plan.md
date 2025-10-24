# Application Orchestrator - Backend Main Loop

## Goal

Replace FormGPS timer with **backend-driven main loop** that coordinates all application operations.

**Key shift:** Control moves from frontend to backend.

## Current Problem

### FormGPS.cs - Timer Watchdog (250ms)

```
FormGPS.tmrWatchdog_Tick (every 250ms = 4 Hz)
  ├─ Business logic (GNSS, vehicle, guidance, sections)
  ├─ Hardware communication (AutoSteer, IMU)
  ├─ UI updates (labels, OpenGL rendering)
  └─ Everything mixed together
```

**Issues:**
- ❌ UI logic mixed with business logic
- ❌ Frontend controls timing
- ❌ Hard to test (requires FormGPS)
- ❌ Impossible to use without WinForms
- ❌ 250ms may be too slow for precision guidance

## Target Solution

### ApplicationOrchestrator Concept

**Backend owns main loop** (IHostedService)
- Frequency: 10 Hz (100ms) - increased from 4 Hz
- Coordinates services in defined order
- Broadcasts state via SignalR to all frontends
- Independent of UI framework

```
Backend: ApplicationOrchestrator (every 100ms)
  ├─► Calls services in order
  ├─► Builds complete state DTO
  └─► Broadcasts to frontends via SignalR

Frontend: FormGPS (passive receiver)
  └─► SignalR handler updates UI only
```

## Architecture

### Service Call Flow

```
Timer Tick (100ms interval)
  │
  ├─► 1. GnssService.GetLatestPositionAsync()
  │       │
  │       └─ Returns: lat, lon, altitude, heading, speed
  │
  ├─► 2. VehicleService.UpdateStateAsync(gnssData)
  │       │
  │       └─ Calculates: vehicle position, heading, roll, pitch
  │
  ├─► 3. GuidanceService.UpdateAsync(vehicleState)
  │       │
  │       └─ Calculates: distance from AB line, steer angle
  │
  ├─► 4. SectionControlService.UpdateAsync(vehicleState)
  │       │
  │       └─ Calculates: which sections on/off
  │
  ├─► 5. AutoSteerService.SendSteerCommandAsync() [if active]
  │       │
  │       └─ Sends: UDP to AutoSteer hardware
  │
  ├─► 6. FieldService.UpdateCoverageAsync() [every 10 ticks]
  │       │
  │       └─ Updates: coverage map
  │
  └─► 7. StateHub.BroadcastState(completeState)
          │
          └─ SignalR: Pushes to all frontends
```

**IMPORTANT:** Order matters! Each service depends on previous results.

## Configuration

### Orchestration Settings

- Main loop frequency: 10 Hz (100ms interval)
- Field coverage update: Every 10 ticks (1 second)
- Performance logging: Enabled
- Configurable via appsettings.json

### Dependency Injection

- Registered as IHostedService
- Starts automatically with backend
- Injected services: GNSS, Vehicle, Guidance, SectionControl, AutoSteer, Field
- Injected: IHubContext<StateHub> for SignalR broadcasts

## Timing & Frequency

### Why 10 Hz?

| Frequency | Interval | Notes |
|-----------|----------|-------|
| 4 Hz | 250ms | **Current (FormGPS)** - may be too slow |
| 10 Hz | 100ms | **Recommended** - good balance |
| 20 Hz | 50ms | High precision, may be overkill |
| 30 Hz | 33ms | Max for GNSS (most are 10 Hz) |

**Decision:** 10 Hz (100ms) - faster than current, not excessive.

### Target Performance

Each tick should complete in < 90ms (90% of 100ms interval)

Rough breakdown:
- GNSS read: < 5ms
- Vehicle update: < 10ms
- Guidance update: < 20ms
- Section control: < 15ms
- AutoSteer send: < 5ms
- State DTO build: < 5ms
- SignalR broadcast: < 20ms (in-process)
- **Total: < 90ms** (10ms safety margin)

## Migration from FormGPS

### Before (FormGPS Timer)

```
FormGPS.tmrWatchdog_Tick()
  ├─ pn.UpdatePosition()             → Business logic
  ├─ guidanceLine.UpdateGuidanceLine() → Business logic
  ├─ section.UpdateSections()        → Business logic
  ├─ mc.SendAutoSteerData()          → Hardware
  ├─ lblSpeed.Text = ...             → UI updates
  └─ oglMain.Invalidate()            → UI updates
```

Everything runs in FormGPS timer (frontend-driven).

### After (Backend Orchestrator)

```
Backend: ApplicationOrchestrator.MainLoopTick()
  ├─ VehicleService.UpdateStateAsync()      → Moved
  ├─ GuidanceService.UpdateAsync()           → Moved
  ├─ SectionControlService.UpdateAsync()     → Moved
  ├─ AutoSteerService.SendSteerCommandAsync() → Moved
  └─ StateHub.BroadcastState()               → New

Frontend: FormGPS.OnStateUpdated(state)
  ├─ lblSpeed.Text = state.Vehicle.Speed     → UI only
  └─ oglMain.Invalidate()                    → UI only
```

Business logic moved to backend, frontend is passive receiver.

### Migration Path

**Phase 1:** Both running (hybrid)
- FormGPS timer: Still active (250ms)
- ApplicationOrchestrator: Running (100ms)
- Feature flag: Controls which is used

**Phase 2:** Gradual flip
- Test ApplicationOrchestrator
- Monitor performance
- Compare results

**Phase 3:** Backend only
- Delete FormGPS timer
- SignalR only

See: [05-adapter-pattern.md](../../architecture/05-adapter-pattern.md) for migration strategy.

## Error Handling

### Service Failures

If GNSS times out:
- Log warning
- Use last known position
- Continue with degraded functionality

If critical error:
- Log error
- Broadcast error state to frontend
- Continue running (don't crash)

### Timer Overrun

If previous tick still running:
- Log warning
- Skip current tick (don't queue)
- Indicates performance issue

## Testing Strategy

### Unit Tests

Test service call order:
- Mock all services
- Verify services called in correct sequence
- Verify SignalR broadcast happens

### Integration Tests

Test full loop:
- Use real services (mock hardware only)
- Verify state updates correctly
- Verify 10 Hz frequency maintained

## References

**Architecture docs:**
- [01-goals.md](../../architecture/01-goals.md) - Phase 1 goals
- [02-strangler-fig.md](../../architecture/02-strangler-fig.md) - Migration pattern
- [03-backend-driven.md](../../architecture/03-backend-driven.md) - Backend ownership
- [04-signalr.md](../../architecture/04-signalr.md) - Communication
- [05-adapter-pattern.md](../../architecture/05-adapter-pattern.md) - Migration strategy

## Tasks

This workflow chunk consists of 6 independent tasks:

1. **task1.md** - Create ApplicationOrchestrator class skeleton
2. **task2.md** - Add timer (100ms, 10 Hz)
3. **task3.md** - Add GNSS → Vehicle coordination
4. **task4.md** - Add Guidance calculation
5. **task5.md** - Add SignalR broadcast
6. **task6.md** - Migration from FormGPS timer

Each task is independent and can be executed separately.
