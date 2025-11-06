# SimulatorService Calculation Review

## 1. Use heading primitives instead of manual radian math
- **Location:** `SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:321-327`
- **Issue:** The tick handler converts the heading to radians, adds the delta, converts back to degrees, and then re-normalizes via `VehiclePhysicsService.NormalizeHeading`. The `Heading` value type already normalizes inputs, so the extra conversions add noise and work.
- **Recommendation:** Convert the change in heading to degrees once (or expose a helper that already returns degrees) and rely on the `Heading` addition operator to normalize.
```csharp
// VehiclePhysicsService exposes a degree-based helper that wraps the existing radian calculation.
var headingChangeDegrees = _physics.CalculateHeadingChangeDegrees(_smoothedSteering, stepDistance);
_currentHeading = _currentHeading + headingChangeDegrees;
```
- **Notes:** With this refactor you can drop the local `RAD_TO_DEG` constant and the `_physics.NormalizeHeading` call entirely.

## 2. Replace magic numbers in the step distance calculation
- **Location:** `SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:318`
- **Issue:** `(_currentSpeed.KilometersPerHour / 3600.0) * 0.093` hides two conversions (km/h → m/s and fixed 93 ms tick) behind unexplained literals.
- **Recommendation:** Introduce a named tick interval and lean on the domain model to express unit conversions explicitly.
```csharp
private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(93);

var distanceMeters = _currentSpeed.ToMetersPerSecond() * TickInterval.TotalSeconds;
var stepDistance = new Distance(distanceMeters);
```
- **Notes:** The explicit constants make it clear how distance is derived and allow the interval to be reused if the timer changes.

## 3. Centralize speed clamping and align with the Speed value object
- **Location:** `SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:101` and `:125`
- **Issue:** The service clamps to `-21` even though the `Speed` struct throws on negative values, and the bounds are duplicated across methods.
- **Recommendation:** Define simulator speed limits once and reuse them through a helper that always returns a valid `Speed`.
```csharp
private const double MinSpeedKph = 0.0;
private const double MaxSpeedKph = 322.0;

private static Speed ClampSpeed(double kmh) =>
    new Speed(Math.Clamp(kmh, MinSpeedKph, MaxSpeedKph));

public void SetSpeed(Speed speed, bool smooth = false)
{
    var clamped = ClampSpeed(speed.KilometersPerHour);
    if (smooth)
    {
        _targetSpeed = clamped;
        _logger.LogDebug("Speed set (smooth): Target={Target:F1} km/h (current={Current:F1} km/h)",
            clamped.KilometersPerHour, _currentSpeed.KilometersPerHour);
    }
    else
    {
        _currentSpeed = clamped;
        _targetSpeed = clamped;
        _logger.LogDebug("Speed set (instant): {Speed:F1} km/h", clamped.KilometersPerHour);
    }
}
```
- **Notes:** Use the same helper inside `AdjustSpeed` so the clamp logic stays consistent in one place.

## 4. Simplify `ReverseDirection` and fix the log template
- **Location:** `SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:205-213`
- **Issue:** The method manually wraps degrees at 360 even though `Heading` can normalize, and the structured log message is missing an opening brace.
- **Recommendation:** Reuse the `Heading` addition operator and emit a valid structured log entry.
```csharp
_currentHeading = _currentHeading + 180.0;
_logger.LogDebug("Direction REVERSED: {Heading:F1} deg", _currentHeading.Degrees);
```
- **Notes:** This removes another magic number branch and keeps the rotation logic consistent with the rest of the service.
