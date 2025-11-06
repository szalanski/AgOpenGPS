# Expose Steering Angle from Backend

## Goal

Fix vehicle "shaking" during movement by exposing backend simulator's smoothed steering angle via new `ApplicationState.Control` property, establishing the Control domain for vehicle actuation feedback.

## Current State

After Workflow 005 (Remove CNMEA Adapter):
- **CSim removed**: Frontend simulator deleted, backend SimulatorService is authoritative
- **Steering angle hardcoded to zero**: Position.designer.cs:161 sets `mc.actualSteerAngleDegrees = 0`
- **Backend has smoothed steering**: SimulatorService._smoothedSteering (line 31) calculated but not exposed
- **CVehicle renders wheels**: Uses `mf.mc.actualSteerAngleDegrees` for wheel angles (lines 185, 219, 246)
- **Visual shake occurs**: Wheels render straight (0°) even when vehicle is turning

**Root Cause Analysis**:
1. **CSim provided smooth steering**: Used `steerangleAve` (averaged over time, lines 35-59)
2. **Workflow 005 removed CSim**: Changed CVehicle from `sim.steerangleAve` to `mc.actualSteerAngleDegrees`
3. **Position.designer.cs hardcodes zero**: Line 161 sets `mc.actualSteerAngleDegrees = 0` with TODO comment
4. **Backend smoothing unused**: SimulatorService._smoothedSteering exists but isn't exposed to frontend
5. **Result**: Vehicle body turns but wheels stay straight, causing visual jitter

## Target State

- **ControlState domain object**: New `ApplicationState.Control` property (like `Gnss` for GPS data)
- **Backend exposes steering**: SimulatorService.GetCurrentSteering() method returns smoothed angle
- **ApplicationOrchestrator broadcasts**: Populates `state.Control.ActualSteeringAngle` on each update
- **Frontend reads backend state**: Position.designer.cs uses `_cachedState.Control?.ActualSteeringAngle.Degrees`
- **CVehicle already compatible**: Automatically picks up backend steering (no changes needed)
- **Visual shake eliminated**: Wheels render at correct angle matching vehicle motion

## Why

**Fix critical bug**: Vehicle shaking breaks visual realism and user experience

**Establish Control domain**: Like `GnssState` groups GPS data, `ControlState` groups vehicle control feedback

**Complete Workflow 005**: Finish steering migration started when CSim was removed

**Enable future migrations**: Control domain ready for sections, auto-steer, implement, IMU

**Architectural consistency**: Backend owns all vehicle state (GPS + Control), frontend renders

**Reuse backend smoothing**: Backend already has steering smoothing algorithm (identical to CSim)

## What This Is NOT

**NOT adding new steering logic**: Backend SimulatorService already calculates smoothed steering

**NOT changing CVehicle**: Wheel rendering code already uses `mc.actualSteerAngleDegrees` correctly

**NOT migrating section control**: Only steering angle (sections come in future workflow)

**NOT changing simulator behavior**: Only exposing existing internal state

**NOT breaking existing features**: Adding new property to ApplicationState (non-breaking change)

## Migration Path

This workflow establishes the Control domain pattern for future vehicle control migrations:

**Phase 1 (This Workflow)**: Expose steering angle only ✅ DONE

**Phase 2 (Workflow 008)**: Add section control states (on/off per section)

**Phase 3 (Future)**: Add auto-steer engagement, implement position, speed control

**Phase 4 (Future)**: Add IMU data (roll, pitch, yaw rate) for horizon indicator

Throughout implementation, existing features continue working - we're exposing backend state, not changing behavior.

## Tasks

This workflow was implemented in 4 phases:

### Phase 1: Create ControlState Model
- Create ControlState.cs in AgOpenGPS.Api.Client/Models/
- Add ActualSteeringAngle property (SteeringAngle value type)
- Add Control property to ApplicationState.cs
- Document purpose and future extensions

### Phase 2: Expose Steering from Backend
- Add GetCurrentSteering() method to SimulatorService.cs
- Inject SimulatorService into ApplicationOrchestrator
- Populate Control.ActualSteeringAngle when broadcasting state

### Phase 3: Update Frontend
- Change Position.designer.cs line 161 to read from `_cachedState.Control`
- Remove TODO comment about backend steering exposure
- Verify CVehicle.cs already compatible (no changes needed)

### Phase 4: Build & Test
- Build client library, backend API, frontend
- Test steering commands change wheel rendering
- Verify no shaking during turns
- Verify wheels align with vehicle heading

## Success Criteria

**ControlState Model**:
- [x] ControlState.cs created in AgOpenGPS.Api.Client/Models/
- [x] ActualSteeringAngle property added (SteeringAngle type)
- [x] XML documentation explains purpose and future extensions
- [x] Follows established pattern (like GnssState)

**ApplicationState Integration**:
- [x] Control property added to ApplicationState.cs
- [x] Documented: "Vehicle control feedback (steering, sections, implement)"
- [x] Nullable (ControlState?) for optional state

**Backend Exposure**:
- [x] SimulatorService.GetCurrentSteering() method added
- [x] Returns _smoothedSteering field (already calculated)
- [x] Public accessor (no internal logic change)

**ApplicationOrchestrator Integration**:
- [x] SimulatorService injected via constructor
- [x] Control state populated when broadcasting
- [x] Control.ActualSteeringAngle set from simulator

**Frontend Update**:
- [x] Position.designer.cs reads from `_cachedState.Control?.ActualSteeringAngle.Degrees`
- [x] Null safety check (defaults to 0 if not available)
- [x] TODO comment removed
- [x] CVehicle.cs already compatible (verified no changes needed)

**Build and Compilation**:
- [x] AgOpenGPS.Api.Client builds successfully
- [x] AgOpenGPS.Api builds successfully
- [x] AgOpenGPS (frontend) builds successfully
- [x] No compilation errors

**Functionality**:
- [x] Steering commands change wheel rendering
- [x] No vehicle shaking during turns
- [x] Wheels align with vehicle heading
- [x] Smooth transitions between steering angles

**Architecture Quality**:
- [x] Control domain established (steering now, sections/auto-steer later)
- [x] Follows GnssState pattern (domain grouping)
- [x] Backend owns control state, frontend renders
- [x] No breaking changes to existing code

## Implementation Notes

**SteeringAngle Value Type Already Exists**: SimulatorService already uses `SteeringAngle` record type - no need to create it

**Backend Smoothing Algorithm**: SimulatorService lines 293-297 smooth steering using identical algorithm to CSim (removed in Workflow 005)

**CVehicle Compatibility**: Lines 185, 219, 246 already use `mf.mc.actualSteerAngleDegrees` - automatically pick up backend steering

**Null Safety**: Frontend checks `_cachedState.Control?.ActualSteeringAngle.Degrees ?? 0` for safe fallback

**No Hardware Changes**: This workflow only affects simulator - real hardware steering angle would come via similar mechanism (future work)

**Future Extensions**:
- Section control states: `Control.Sections.IsSectionOn[]`
- Auto-steer engagement: `Control.IsAutoSteerEngaged`
- Implement position: `Control.Implement.Position`
- Speed feedback: `Control.Speed.Actual` vs `Control.Speed.Target`
- IMU data: `Control.Imu.Roll`, `Control.Imu.Pitch`, `Control.Imu.YawRate`

## Files Created

1. **SourceCode/AgOpenGPS.Api.Client/Models/ControlState.cs** (30 lines)
   - Vehicle control feedback state model
   - ActualSteeringAngle property (SteeringAngle type)
   - XML documentation for future extensions
   - Default initializer (SteeringAngle.Zero)

2. **docs/workflow/007-expose-steering-angle/plan-007-expose-steering-angle.md** (this file)
   - Complete workflow documentation
   - Root cause analysis
   - Implementation phases
   - Success criteria
   - Future extensions

## Files Modified

1. **SourceCode/AgOpenGPS.Api.Client/Models/ApplicationState.cs** (line ~31)
   - Added: `public ControlState? Control { get; set; }`
   - Documented: "Vehicle control feedback (steering, sections, implement)"

2. **SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs** (after line 60)
   - Added: `public SteeringAngle GetCurrentSteering() => _smoothedSteering;`
   - Exposes smoothed steering angle to ApplicationOrchestrator

3. **SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs** (lines 17, 24, 30, 80)
   - Injected SimulatorService via constructor (line 17, 24, 30)
   - Populate Control state when broadcasting (line 80):
     ```csharp
     Control = new ControlState
     {
         ActualSteeringAngle = _simulatorService.GetCurrentSteering()
     }
     ```

4. **SourceCode/GPS/Forms/Position.designer.cs** (lines 159-161)
   - Changed from: `mc.actualSteerAngleDegrees = 0;`
   - Changed to: `mc.actualSteerAngleDegrees = _cachedState.Control?.ActualSteeringAngle.Degrees ?? 0;`
   - Removed TODO comment about backend steering exposure

5. **docs/README.md** (after workflow 006 entry)
   - Added workflow 007 entry with "COMPLETED" status
   - Listed key implementations
   - Documented outcome

6. **CLAUDE.md** (status section, line ~139)
   - Updated status to include workflow 007

## Testing Instructions

1. Stop any running processes (Backend API, Frontend, Visual Studio)
2. Rebuild solution: `dotnet build SourceCode/AgOpenGPS.sln`
3. Start backend: `dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj`
4. Start frontend: `dotnet run --project SourceCode/GPS/AgOpenGPS.csproj`
5. Use simulator controls to change steering (Left/Right arrow keys)
6. Verify logs show:
   - Backend: Broadcasting Control state with steering angle
   - Frontend: Rendering wheels at correct angle
7. Visual verification:
   - Vehicle wheels turn left/right matching steering input
   - No shaking or jitter during turns
   - Smooth transitions between steering angles
   - Wheels align with vehicle heading when going straight

## Next Steps

**Workflow 008 (Future)**: Expose Section Control States
- Add `SectionControlState` class with per-section on/off states
- Extend `ControlState.Sections` property
- Migrate CSection.cs to use backend state
- Enable section control visualization from backend

**Auto-Steer Integration**:
- Add `ControlState.IsAutoSteerEngaged` boolean
- Add `ControlState.TargetSteeringAngle` for steering curve display
- Expose auto-steer corrections from backend

**IMU Data Integration**:
- Add `ImuState` nested object in ControlState
- Expose roll, pitch, yaw rate from AHRS
- Enable horizon indicator, tilt compensation

**Hardware Support**:
- Extend GetCurrentSteering() to read from CModuleComm (real hardware)
- Support both simulator and hardware steering sources
- Add source indicator (simulator vs hardware)

## Root Cause Details (Technical)

**Old Code (before Workflow 005)** - CVehicle.cs line 185:
```csharp
AckermannAngles(
    -(mf.timerSim.Enabled ? mf.sim.steerangleAve : mf.mc.actualSteerAngleDegrees),
    out double leftAckermann,
    out double rightAckermann);
```

**New Code (after Workflow 005)** - CVehicle.cs line 185:
```csharp
AckermannAngles(
    -mf.mc.actualSteerAngleDegrees,
    out double leftAckermann,
    out double rightAckermann);
```

**CSim Steering Smoothing** (removed in Workflow 005) - CSim.cs lines 35-59:
```csharp
double diff = Math.Abs(steerAngle - steerangleAve);

if (diff > 11)
{
    if (steerangleAve >= steerAngle) steerangleAve -= 6;
    else steerangleAve += 6;
}
else if (diff > 5)
{
    if (steerangleAve >= steerAngle) steerangleAve -= 2;
    else steerangleAve += 2;
}
else if (diff > 1)
{
    if (steerangleAve >= steerAngle) steerangleAve -= 0.5;
    else steerangleAve += 0.5;
}
else
{
    steerangleAve = steerAngle;
}
```

**Backend Has Same Algorithm** - VehiclePhysicsService.SmoothSteeringAngle():
- Identical smoothing algorithm as CSim
- Already implemented in backend since Workflow 002
- Result stored in SimulatorService._smoothedSteering
- Just needed to be exposed to frontend!
