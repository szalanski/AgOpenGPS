# Workflow 007: Critical Fixes Applied

## Summary
Fixed **two critical bugs** causing vehicle shaking in the backend simulator:
1. **Steering unit mismatch** - heading calculation received km instead of metres
2. **Local plane jitter** - coordinate transformer used placeholder origin instead of actual start position

---

## Fix 1: Steering Unit Mismatch

### Problem
- `SimulatorService` calculated step distance in **kilometres**
- `VehiclePhysicsService.CalculateHeadingChange()` expected **metres**
- Result: Heading changes were 1000× too small, causing guidance oscillation

### Solution
**File**: `SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:306-314`

```csharp
// Calculate step distance from speed (93ms tick, speed in km/h)
_stepDistance = (_currentSpeed.KilometersPerHour / 3600.0) * 0.093; // kilometres

// CRITICAL FIX: Convert to metres for heading calculation
double stepDistanceMeters = _stepDistance * 1000.0;

// Update heading based on steering
double headingChange = _physics.CalculateHeadingChange(_smoothedSteering.Degrees, stepDistanceMeters);

// Position calculation still uses kilometres
var (newLat, newLon) = _physics.CalculateNewPosition(..., _stepDistance);
```

**Documentation**: Added XML comments to `VehiclePhysicsService.CalculateHeadingChange()` specifying metres requirement

---

## Fix 2: Local Plane Initialization

### Problem
- `ApplicationOrchestrator` initialized local plane to placeholder `(45.0, -93.0)`
- Simulator started at user-selected coordinates (e.g., 51°N, 21°E)
- Result: Coordinate offsets of hundreds of kilometres, floating-point precision loss, rendering jitter

### Solution
**File**: `SourceCode/AgOpenGPS.Api/Commands/Handlers/UpdateSimulatorCommandHandler.cs:39-50`

```csharp
// CRITICAL FIX: Initialize local plane when simulator starts
if (request.Event.Type == SimulatorEventType.Start && request.Event.StartData != null)
{
    var origin = request.Event.StartData.Position;
    _gnssService.InitializeLocalPlane(origin);
    _coordinateService.InitializeLocalPlane(origin);

    _logger.LogInformation("Local plane initialized to simulator start position: ({Lat:F6}, {Lon:F6})",
        origin.Latitude, origin.Longitude);
}
```

**Changes**:
- Injected `IGnssService` and `ICoordinateService` into handler
- Intercept `Start` commands before delegating to `SimulatorService`
- Initialize both services with actual start position

---

## Expected Results

### Before Fixes
- ❌ Vehicle barely turns despite steering input
- ❌ Guidance system aggressively overcorrects
- ❌ Visible "shaking" during movement
- ❌ World grid "swims" due to coordinate jitter
- ❌ Local coordinates show hundreds of km offset

### After Fixes
- ✅ Vehicle turns smoothly with steering input
- ✅ Heading changes match expected turn rate
- ✅ No visible oscillation during movement
- ✅ Stable world grid rendering
- ✅ Local coordinates near zero (precision preserved)

---

## Testing Recommendations

1. **Manual Simulator Test**:
   - Start simulator at custom coordinates (not 45°, -93°)
   - Apply steering input (e.g., 10° left)
   - Verify smooth turning without oscillation
   - Check local coordinates remain near zero

2. **Integration Tests**:
   - Run `SimulatorIntegrationTests.cs`
   - Verify steering-related tests now pass
   - Check position updates match expected physics

3. **Auto-Steer Test**:
   - Enable guidance line
   - Verify vehicle follows line smoothly
   - No aggressive corrections or shaking

---

## Related Documentation

- Root cause analysis: [docs/backend-simulator-shaking.md](../backend-simulator-shaking.md)
- Workflow plan: [docs/workflow/007-expose-steering-angle/plan-007-expose-steering-angle.md](plan-007-expose-steering-angle.md)
- Architecture context: [docs/architecture/03-backend-driven.md](../../architecture/03-backend-driven.md)
