# Task 4: Add Guidance & Section Control

## Objective

Add guidance calculation and section control to main loop.

## Context

Next two services in the chain: GuidanceService calculates steering, SectionControlService manages implement sections.

See: [plan.md](plan.md) for service call order.

## What to Implement

### Service Call Sequence in MainLoopTick()

```
3. GuidanceService.UpdateAsync(vehicleState)
   ↓
4. SectionControlService.UpdateAsync(vehicleState)
   ↓
5. AutoSteerService.SendSteerCommandAsync() [if active]
```

### GuidanceService

Calculates:
- Distance from active AB line
- Steer angle
- Lookahead point
- Heading error

Returns: GuidanceState

### SectionControlService

Calculates:
- Which sections should be on/off
- Based on: vehicle position, boundaries, coverage map
- Updates section relay states

Returns: SectionsState

### AutoSteerService

**Only if** guidance is active:
- Sends steer command to hardware (UDP)
- Uses: steer angle + vehicle speed
- Non-blocking send

### Throttling

Field coverage update (less frequent):
```
if (_tickCounter % 10 == 0)  // Every 10 ticks = 1 second
{
    await _fieldService.UpdateCoverageAsync(vehicleState.Position);
}
```

## Acceptance Criteria

- ✅ GuidanceService called every tick with vehicle state
- ✅ SectionControlService called every tick
- ✅ AutoSteerService called only when active
- ✅ FieldService called every 1 second (10 ticks)
- ✅ Service call order maintained
- ✅ Performance < 50ms for all service calls
- ✅ Errors handled gracefully

## Testing

### Unit Test

Mock services to verify:
- Call order: Guidance → Sections → AutoSteer
- Vehicle state passed correctly
- AutoSteer skipped when not active
- Field coverage throttled (every 10 ticks)

### Integration Test

With mock hardware:
- Set up AB line
- Move vehicle off line
- Verify guidance calculates steer angle
- Verify sections turn on/off correctly
- Verify field coverage updates

### Manual Test

Run backend with simulator:
- Draw AB line
- Drive vehicle
- Verify steering commands sent
- Verify sections control working

## Service Interfaces

Ensure these exist:

```csharp
public interface IGuidanceService
{
    Task<GuidanceState> UpdateAsync(VehicleState vehicle);
}

public interface ISectionControlService
{
    Task<SectionsState> UpdateAsync(VehicleState vehicle);
}

public interface IAutoSteerService
{
    Task SendSteerCommandAsync(double steerAngle, double speed);
}

public interface IFieldService
{
    Task UpdateCoverageAsync(Vector3 position);
    FieldState GetCurrentFieldState();
}
```

## Performance Budget

Target times:
- Guidance: < 20ms
- Section control: < 15ms
- AutoSteer send: < 5ms
- Field coverage: < 10ms (every 10 ticks)

**Total: < 50ms** (50% of budget)

## Notes

- Guidance depends on vehicle state (must run after task3)
- AutoSteer is conditional (check IsAutoSteerActive)
- Field coverage throttled to reduce load
