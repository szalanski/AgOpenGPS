# Task 5: Delete CNMEA Adapter

## Goal

Delete the CNMEA adapter method (`UpdateFromBackendState`) and remove the adapter call from FormGPS now that all code reads directly from ApplicationState.

## Context

The CNMEA adapter was a temporary bridge pattern introduced in Workflow 002 to enable gradual migration:

```csharp
// FormGPS.cs (line 607)
pn.UpdateFromBackendState(state);

// CNMEA.cs (lines 37-60)
public void UpdateFromBackendState(ApplicationState state)
{
    fix.easting = state.Gnss.LocalPosition.Easting;
    fix.northing = state.Gnss.LocalPosition.Northing;
    speed = state.Gnss.Speed.KilometersPerHour;
    // ... 7 more mappings
}
```

After Tasks 1-4, all pn reads have been replaced with direct ApplicationState access. The adapter is now dead code.

## Current State

**CNMEA.cs** (SourceCode/GPS/Classes/CNMEA.cs, lines 37-60):
- `UpdateFromBackendState()` method maps 10 backend properties to 10 legacy fields
- 24 lines total (including comments)
- No longer called by anything after refactoring

**FormGPS.cs** (SourceCode/GPS/Forms/FormGPS.cs, line 607):
- Calls `pn.UpdateFromBackendState(state)` in OnStateReceived()
- This is the only remaining call site

## Target State

- UpdateFromBackendState() method deleted from CNMEA.cs
- Adapter call removed from FormGPS.cs
- No dead code remaining
- Application builds and runs correctly

## Implementation Steps

### Step 1: Verify No Remaining pn Reads

Before deleting adapter, confirm all tasks 1-4 complete:

```bash
# Should find ZERO pn reads in these files (except CNMEA.cs itself)
grep -rn "pn\." SourceCode/GPS/Forms/Position.Designer.cs
grep -rn "pn\." SourceCode/GPS/Classes/CContour.cs
grep -rn "pn\." SourceCode/GPS/Forms/GUI.Designer.cs

# Should find NO sim references
grep -rn "\.sim\b\|CSim" SourceCode/GPS/Forms/FormGPS.cs
```

If any found, complete refactoring before proceeding.

### Step 2: Remove Adapter Call from FormGPS

**FormGPS.cs OnStateReceived method** (line ~589-613):

**Before**:
```csharp
private void OnStateReceived(object sender, ApplicationState state)
{
    if (InvokeRequired)
    {
        Invoke(new Action<object, ApplicationState>(OnStateReceived), sender, state);
        return;
    }

    // Cache the latest state
    _cachedState = state;

    // Update legacy CNMEA fields via adapter
    pn.UpdateFromBackendState(state);  // ← DELETE THIS LINE

    // ... other code
}
```

**After**:
```csharp
private void OnStateReceived(object sender, ApplicationState state)
{
    if (InvokeRequired)
    {
        Invoke(new Action<object, ApplicationState>(OnStateReceived), sender, state);
        return;
    }

    // Cache the latest state
    _cachedState = state;

    // Adapter removed - all code reads from _cachedState directly

    // ... other code
}
```

### Step 3: Delete UpdateFromBackendState Method

**CNMEA.cs** (lines 37-60):

**Before**:
```csharp
/// <summary>
/// Adapter pattern: Map backend ApplicationState → legacy CNMEA fields.
/// Enables zero-breaking-changes migration (Strangler Fig).
/// </summary>
public void UpdateFromBackendState(ApplicationState state)
{
    if (state?.Gnss == null) return;

    fix.easting = state.Gnss.LocalPosition.Easting;
    fix.northing = state.Gnss.LocalPosition.Northing;
    speed = state.Gnss.Speed.KilometersPerHour;
    vtgSpeed = state.Gnss.Speed.KilometersPerHour;
    altitude = state.Gnss.Altitude.Meters;
    headingTrue = state.Gnss.HeadingSingle.Degrees;
    headingTrueDual = state.Gnss.HeadingDual.Degrees;
    fixQuality = state.Gnss.Quality.FixQuality;
    satellitesTracked = state.Gnss.Quality.SatellitesTracked;
    hdop = state.Gnss.Quality.Hdop;
    age = state.Gnss.Quality.Age;
}
```

**After**:
```csharp
// Method deleted entirely
```

Delete lines 37-60 (or the exact range containing UpdateFromBackendState).

### Step 4: Check for ApplicationState Import

Since UpdateFromBackendState used ApplicationState type, check if CNMEA.cs still needs the import:

```csharp
using AgOpenGPS.Api.Client.Models;  // ← May no longer be needed
```

**If no other code in CNMEA uses ApplicationState**:
- Remove the using statement

**If other code uses ApplicationState**:
- Keep the using statement

### Step 5: Search for Any Other Adapter References

Search entire codebase for UpdateFromBackendState:

```bash
grep -rn "UpdateFromBackendState" SourceCode/GPS/
```

**Expected**: Zero results after deletion.

If any found:
- Delete those calls too
- Or refactor to use _cachedState directly

### Step 6: Build and Test

```bash
# Build
dotnet build SourceCode/AgOpenGPS.sln

# Start backend + FormGPS
dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj &
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj
```

Verify:
- No compilation errors mentioning UpdateFromBackendState
- Application starts correctly
- GPS data flows from backend → _cachedState → UI
- No runtime errors

## Verification

**Code Verification**:
- [ ] UpdateFromBackendState() method deleted from CNMEA.cs
- [ ] `pn.UpdateFromBackendState(state);` call removed from FormGPS.cs
- [ ] No other references to UpdateFromBackendState in codebase
- [ ] Unused using statements removed (if applicable)

**Build Verification**:
- [ ] No compilation errors
- [ ] No warnings about missing methods
- [ ] Solution builds successfully

**Runtime Verification**:
- [ ] FormGPS starts without errors
- [ ] No "UpdateFromBackendState not found" errors
- [ ] No null reference exceptions
- [ ] Backend state flows correctly to _cachedState

**Functional Verification**:
- [ ] GPS data displays in UI correctly
- [ ] Position updates smoothly
- [ ] Speed, heading, satellites all display
- [ ] No degradation in functionality

## Edge Cases

**Forgotten pn Reads**:
- If any code still reads pn.* fields, it will show stale data
- pn fields no longer updated after adapter deletion
- Search thoroughly in Step 1 to avoid this

**Circular Dependencies**:
- CNMEA.cs may reference FormGPS (mf parameter)
- FormGPS references CNMEA (pn field)
- Adapter deletion doesn't affect this (adapter was one-way)

## Rollback Plan

If issues arise:

```bash
# Restore CNMEA.cs
git checkout HEAD -- SourceCode/GPS/Classes/CNMEA.cs

# Restore FormGPS.cs
git checkout HEAD -- SourceCode/GPS/Forms/FormGPS.cs
```

Then investigate:
- Were any pn reads missed in refactoring?
- Is there code path still using UpdateFromBackendState?
- Run comprehensive grep search to find missed references

## Expected Outcome

After this task:
- ✅ CNMEA adapter deleted (24 lines removed)
- ✅ FormGPS no longer calls UpdateFromBackendState
- ✅ Dead code eliminated
- ✅ Cleaner codebase
- ✅ All GPS data flows: Backend → _cachedState → Application code

## Files Modified

**Modified**:
- `SourceCode/GPS/Classes/CNMEA.cs` (delete UpdateFromBackendState method, lines 37-60)
- `SourceCode/GPS/Forms/FormGPS.cs` (remove adapter call, line 607)

## Next Task

Proceed to [task6-decide-cnmea-fate.md](task6-decide-cnmea-fate.md) to decide whether to delete CNMEA class entirely or keep it as minimal stub.
