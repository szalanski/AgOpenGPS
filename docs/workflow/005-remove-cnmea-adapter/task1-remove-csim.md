# Task 1: Remove CSim Frontend Simulator

## Goal

Delete the frontend simulator (CSim) since the backend SimulatorService is now the authoritative GPS data source.

## Context

After Workflow 002, we have two simulators:
1. **Backend SimulatorService** - Generates GPS data via UDP packets (93ms tick, DDD architecture)
2. **Frontend CSim** - Legacy simulator that writes directly to `pn.*` fields

The frontend simulator is now redundant and creates confusion. Backend simulator is the proven, tested replacement.

## Current State

**CSim.cs** (SourceCode/GPS/Classes/CSim.cs):
- 197 lines total
- DoSimTick() method (lines 71-90) writes 9 pn properties:
  - `pn.vtgSpeed`
  - `pn.fix.northing`
  - `pn.fix.easting`
  - `pn.headingTrue`
  - `pn.headingTrueDual`
  - `pn.hdop`
  - `pn.altitude`
  - `pn.satellitesTracked`
  - `ahrs.imuHeading`

**FormGPS references to CSim**:
```csharp
// Line 162: Field declaration
public CSim sim;

// Line 348: Instantiation
sim = new CSim(this);

// Unknown lines: Possible method calls to sim.DoSimTick()
```

## Target State

- CSim.cs file deleted
- All FormGPS references to `sim` removed
- Backend simulator is the only GPS data source
- Development/testing uses backend simulator exclusively

## Implementation Steps

### Step 1: Find All CSim References

Search for all references to CSim and sim in FormGPS:

```bash
# Find all references
grep -n "CSim\|\.sim\b" SourceCode/GPS/Forms/FormGPS.cs
grep -n "CSim\|\.sim\b" SourceCode/GPS/Forms/*.Designer.cs
```

Document every location where CSim is used.

### Step 2: Remove FormGPS References

Remove all CSim usage from FormGPS.cs and Designer files:

1. Remove field declaration (line 162):
   ```csharp
   public CSim sim;  // ← DELETE
   ```

2. Remove instantiation (line 348):
   ```csharp
   sim = new CSim(this);  // ← DELETE
   ```

3. Remove any calls to `sim.DoSimTick()` or similar methods

4. Remove any conditional checks like `if (sim != null)`

### Step 3: Check for Other Dependencies

Search entire GPS project for CSim usage:

```bash
# Search all files
grep -rn "CSim" SourceCode/GPS/Classes/
grep -rn "CSim" SourceCode/GPS/Forms/
```

If found in other files:
- Document the usage
- Remove or refactor to use backend simulator
- Ensure no circular dependencies

### Step 4: Delete CSim.cs

Once all references removed:

```bash
# Delete the file
rm SourceCode/GPS/Classes/CSim.cs
```

### Step 5: Update Project File (if needed)

Check if CSim.cs is explicitly listed in GPS.csproj:

```bash
grep -n "CSim" SourceCode/GPS/AgOpenGPS.csproj
```

If found, remove the entry.

### Step 6: Build and Test

```bash
# Build the solution
dotnet build SourceCode/AgOpenGPS.sln

# Verify no compilation errors
# Start backend simulator
dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj

# Start FormGPS
dotnet run --project SourceCode/GPS/AgOpenGPS.csproj

# Verify GPS data flows from backend simulator
# Check UI shows position/speed/heading updates
```

## Verification

**Build Verification**:
- [ ] No compilation errors mentioning CSim
- [ ] GPS.csproj builds successfully
- [ ] No missing type references

**Runtime Verification**:
- [ ] FormGPS starts without errors
- [ ] Backend simulator sends GPS data
- [ ] UI displays GPS data (speed, position, heading)
- [ ] No null reference exceptions
- [ ] No "sim not found" errors in logs

**Functional Verification**:
- [ ] Backend simulator controls work (start/stop/speed/steering)
- [ ] GPS frequency shows ~10 Hz
- [ ] Position updates smoothly
- [ ] Heading changes when steering applied

## Rollback Plan

If issues arise:

1. **Restore CSim.cs** from git history
2. **Restore FormGPS references** (field, instantiation)
3. **Investigate why backend simulator insufficient**
4. **Document gaps before retrying**

Git commands:
```bash
# Restore deleted file
git checkout HEAD -- SourceCode/GPS/Classes/CSim.cs

# Restore FormGPS changes
git checkout HEAD -- SourceCode/GPS/Forms/FormGPS.cs
```

## Expected Outcome

After this task:
- ✅ CSim.cs deleted
- ✅ No frontend simulator code
- ✅ Backend simulator is single source of GPS data
- ✅ FormGPS receives GPS data via SignalR only
- ✅ Development workflow uses backend simulator
- ✅ Codebase simplified by ~200 lines

## Files Modified

**Deleted**:
- `SourceCode/GPS/Classes/CSim.cs`

**Modified**:
- `SourceCode/GPS/Forms/FormGPS.cs` (remove sim field and instantiation)
- Possibly: `SourceCode/GPS/AgOpenGPS.csproj` (remove CSim.cs reference if explicit)

## Next Task

Proceed to [task2-refactor-position-updatefixposition.md](task2-refactor-position-updatefixposition.md) to refactor Position.UpdateFixPosition() to read from ApplicationState instead of pn fields.
