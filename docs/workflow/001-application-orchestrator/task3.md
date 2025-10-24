# Task 3: Add GNSS → Vehicle Coordination

## Objective

Add GNSS data acquisition and vehicle state update to main loop.

## Context

First two services in the call chain: GnssService reads hardware data, VehicleService calculates vehicle state.

See: [plan.md](plan.md) for service call order.

## What to Implement

### Service Call Sequence in MainLoopTick()

```
1. GnssService.GetLatestPositionAsync()
   ↓
2. VehicleService.UpdateStateAsync(gnssData)
```

**Order matters:** Vehicle depends on GNSS data.

### MainLoopTick() Changes

Add inside try/catch block:

1. Call `_gnssService.GetLatestPositionAsync()`
   - Returns: GnssPosition (lat, lon, altitude, heading, speed)

2. Call `_vehicleService.UpdateStateAsync(gnssData)`
   - Uses: GNSS position + IMU data + antenna offset
   - Returns: VehicleState (position, heading, roll, pitch)

### Error Handling

**GNSS timeout:**
- Catch GnssTimeoutException
- Log warning: "GNSS timeout, using last known position"
- Call `_vehicleService.GetLastKnownState()`
- Continue with degraded functionality

**Other errors:**
- Catch Exception
- Log error
- Continue loop (don't crash)

### Performance Measurement

- Measure elapsed time for service calls
- Log warning if > 15ms (15% of budget)

## Acceptance Criteria

- ✅ GnssService called every tick
- ✅ VehicleService called with GNSS data
- ✅ GNSS timeout handled gracefully
- ✅ Vehicle state updated at 10 Hz
- ✅ Performance measured and logged
- ✅ Errors don't crash main loop

## Testing

### Unit Test

Mock services to verify:
- Call order (GNSS → Vehicle)
- GNSS data passed to Vehicle
- Timeout handling works

### Integration Test

With mock hardware:
- Inject fake GNSS data
- Verify vehicle state updates
- Simulate timeout
- Verify fallback to last known state

### Manual Test

Run backend with real/simulated GNSS:
- Verify logs show vehicle updates
- Check performance < 15ms

## Service Interfaces

Ensure these exist:

```csharp
public interface IGnssService
{
    Task<GnssPosition> GetLatestPositionAsync();
}

public interface IVehicleService
{
    Task<VehicleState> UpdateStateAsync(GnssPosition gnssData);
    VehicleState GetLastKnownState();
}
```

## Notes

- Use async/await (services may do I/O)
- MainLoopTick signature: `async void` (timer callback)
- Don't block on long operations
