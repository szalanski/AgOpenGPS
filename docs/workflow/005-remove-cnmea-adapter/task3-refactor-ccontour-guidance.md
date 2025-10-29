# Task 3: Refactor CContour Guidance to Use ApplicationState

## Goal

Refactor CContour.cs guidance calculations to read position from `_cachedState.Gnss.LocalPosition` instead of `pn.fix` fields.

## Context

CContour implements contour-following guidance for terraced fields. It calculates distance from the vehicle to the contour line using the vehicle's current position.

Currently reads from `pn.fix.easting` and `pn.fix.northing` (line 684). After this refactoring, it will read from ApplicationState like all other GPS consumers.

## Current State

**CContour.cs** (SourceCode/GPS/Classes/CContour.cs, line 684):

```csharp
// Calculate distance from current line pivot
distanceFromCurrentLinePivot = ((dy * mf.pn.fix.easting) - (dx * mf.pn.fix.northing) + ...);
```

**Context**:
- `mf` = FormGPS reference (constructor parameter)
- `mf.pn` = CNMEA instance
- `mf.pn.fix.easting/northing` = vehicle position in local coordinates (meters)

## Target State

CContour reads from `mf._cachedState.Gnss.LocalPosition`:

```csharp
distanceFromCurrentLinePivot = ((dy * mf._cachedState.Gnss.LocalPosition.Easting)
                                - (dx * mf._cachedState.Gnss.LocalPosition.Northing) + ...);
```

## Implementation Steps

### Step 1: Read Current Implementation

Read CContour.cs to understand the full context:

```bash
Read SourceCode/GPS/Classes/CContour.cs
```

Find:
- Line 684 and surrounding context
- Any other pn references in the file
- Method name where line 684 appears
- Calculation purpose

### Step 2: Locate All pn References in CContour

Search for all pn usage:

```bash
grep -n "pn\." SourceCode/GPS/Classes/CContour.cs
```

Document every occurrence:
- Line number
- Property accessed (easting/northing/speed/etc.)
- Context (what calculation)

### Step 3: Replace Position Reads

For each pn.fix reference, replace with ApplicationState:

**Before**:
```csharp
double easting = mf.pn.fix.easting;
double northing = mf.pn.fix.northing;
```

**After**:
```csharp
double easting = mf._cachedState.Gnss.LocalPosition.Easting;
double northing = mf._cachedState.Gnss.LocalPosition.Northing;
```

**Specific replacement for line 684**:

**Before**:
```csharp
distanceFromCurrentLinePivot = ((dy * mf.pn.fix.easting) - (dx * mf.pn.fix.northing) + ...);
```

**After**:
```csharp
distanceFromCurrentLinePivot = ((dy * mf._cachedState.Gnss.LocalPosition.Easting)
                                - (dx * mf._cachedState.Gnss.LocalPosition.Northing) + ...);
```

### Step 4: Add Null Safety (if needed)

If CContour methods can be called before GPS data available, add guards:

```csharp
public void CalculateGuidance()
{
    // Guard against null state
    if (mf._cachedState?.Gnss?.LocalPosition == null)
    {
        return;  // Or set default guidance values
    }

    // Rest of calculation...
}
```

### Step 5: Check for Speed or Other GPS Reads

If CContour reads other pn properties, replace them:

| Legacy pn Field | ApplicationState Replacement |
|----------------|------------------------------|
| `pn.speed` | `_cachedState.Gnss.Speed.KilometersPerHour` |
| `pn.headingTrue` | `_cachedState.Gnss.HeadingSingle.Degrees` |
| `pn.altitude` | `_cachedState.Gnss.Altitude.Meters` |

### Step 6: Verify No More pn Reads

```bash
# Should return ZERO results after refactoring
grep -n "pn\." SourceCode/GPS/Classes/CContour.cs
```

### Step 7: Build and Test

```bash
# Build
dotnet build SourceCode/AgOpenGPS.sln

# Start backend + FormGPS
dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj &
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
```

Test contour guidance:
- Load a field with contour lines
- Enable contour following mode
- Verify guidance calculations work
- Check cross-track error displays correctly

## Verification

**Code Verification**:
- [ ] All `mf.pn.fix.easting` replaced with `mf._cachedState.Gnss.LocalPosition.Easting`
- [ ] All `mf.pn.fix.northing` replaced with `mf._cachedState.Gnss.LocalPosition.Northing`
- [ ] Any other pn reads replaced with ApplicationState equivalents
- [ ] Null guards added if necessary
- [ ] No pn references remain in CContour.cs

**Build Verification**:
- [ ] No compilation errors
- [ ] No warnings about unused variables
- [ ] Solution builds successfully

**Runtime Verification**:
- [ ] FormGPS starts without errors
- [ ] No null reference exceptions when contour guidance active
- [ ] No "LocalPosition is null" errors

**Functional Verification** (if possible to test):
- [ ] Contour guidance activates correctly
- [ ] Cross-track error calculated correctly
- [ ] Guidance line displays on screen
- [ ] Vehicle follows contour path as before
- [ ] No sudden jumps or incorrect calculations

## Edge Cases

**No GPS Fix**:
- If GPS quality poor, LocalPosition may be (0,0)
- Null guard prevents crashes
- Guidance should disable or show "No GPS"

**Backend Disconnected**:
- `_cachedState` retains last values
- Contour guidance continues with stale position (same as before)
- Reconnection resumes normal operation

**High-Speed Updates**:
- Contour calculations called on every GPS update (~10 Hz)
- Same frequency as before, just different data source
- No performance impact expected

## Rollback Plan

If contour guidance breaks:

```bash
# Restore original file
git checkout HEAD -- SourceCode/GPS/Classes/CContour.cs
```

Then investigate:
- Are LocalPosition values different from pn.fix?
- Is _cachedState null when guidance runs?
- Are calculations producing incorrect results?
- Log both pn and ApplicationState values to compare

## Expected Outcome

After this task:
- ✅ CContour reads from ApplicationState, not pn
- ✅ Contour guidance calculations work correctly
- ✅ No runtime errors
- ✅ One more dependency on CNMEA removed
- ✅ Consistent data access pattern across guidance classes

## Files Modified

**Modified**:
- `SourceCode/GPS/Classes/CContour.cs` (line 684 and any other pn reads)

## Next Task

Proceed to [task4-remove-legacy-ui-guards.md](task4-remove-legacy-ui-guards.md) to remove backend connection guards in GUI code.
