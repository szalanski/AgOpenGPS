# Remove CNMEA Adapter and Frontend Simulator

## Goal

Remove the CNMEA adapter pattern and frontend simulator (CSim) now that GPS/GNSS processing has been fully migrated to the backend, revealing how much frontend logic remains to be migrated.

## Current State

After Workflow 002 (GPS/GNSS Migration):
- **Backend owns GPS processing**: GnssService processes UDP packets, ApplicationOrchestrator broadcasts state via SignalR
- **CNMEA adapter bridges backend → legacy**: `UpdateFromBackendState()` maps 10 backend properties to 10 legacy fields
- **CSim frontend simulator**: Writes to `pn.*` fields directly (9 properties in DoSimTick)
- **Position.UpdateFixPosition()**: Reads legacy `pn.speed`, `pn.fix.*`, `pn.hdop` (360+ lines)
- **CContour guidance**: Reads `pn.fix.easting/northing` for path calculations
- **GUI.Designer.cs**: Legacy guards protect UI updates when backend connected

**Active CNMEA Dependencies**:
1. CSim.cs (lines 71-90) - Writes 9 pn properties
2. Position.Designer.cs (UpdateFixPosition) - Reads pn fields
3. CContour.cs (line 684) - Reads pn.fix for guidance
4. GUI.Designer.cs (tmrWatchdog) - Legacy UI updates (guarded)
5. FormGPS.cs (line 607) - Calls adapter UpdateFromBackendState()

## Target State

- **CSim removed**: Frontend simulator deleted, backend SimulatorService is the only simulator
- **Direct ApplicationState access**: All code reads from `_cachedState.Gnss` instead of `pn.*`
- **CNMEA adapter deleted**: `UpdateFromBackendState()` method removed
- **Legacy guards removed**: No more backend connection checks in UI code
- **CNMEA class fate**: Either deleted entirely or kept as minimal domain stub (if needed)
- **Clear visibility**: Know exactly what frontend business logic remains after GPS removal

## Why

**Simplify codebase**: Remove 200+ lines of adapter/legacy code no longer needed

**Reveal remaining work**: See clearly what frontend logic still needs backend migration

**Single simulator path**: Backend simulator is authoritative, no frontend duplication

**Type safety**: Use strongly-typed ApplicationState instead of mutable pn fields

**Prepare for next migrations**: Clean slate for Section Control, Guidance, and Field migrations

**Validate architecture**: Prove Strangler Fig pattern works - old code paths fully replaced

## What This Is NOT

**NOT migrating new business logic**: Section control, guidance, boundaries stay in frontend (later workflows)

**NOT removing OpenGL rendering**: Visualization stays in frontend entirely

**NOT changing backend**: Backend API unchanged, only frontend refactoring

**NOT adding features**: Pure removal/refactoring, no new capabilities

**NOT breaking tests**: All integration tests remain passing

## Migration Path

This workflow completes the GPS/GNSS migration started in Workflow 002:

**Phase 1 (Workflow 002)**: Backend GPS processing + CNMEA adapter for compatibility ✅ DONE

**Phase 2 (This Workflow)**: Remove adapter and refactor frontend to use ApplicationState directly

**Phase 3 (Future Workflows)**: Migrate remaining business logic (section control, guidance, etc.)

Throughout migration, FormGPS remains functional - we're removing dead code, not breaking features.

## Tasks

1. [task1-remove-csim.md](task1-remove-csim.md) - Delete CSim frontend simulator (backend simulator is authoritative)
2. [task2-refactor-position-updatefixposition.md](task2-refactor-position-updatefixposition.md) - Replace pn field reads with ApplicationState in Position.Designer.cs
3. [task3-refactor-ccontour-guidance.md](task3-refactor-ccontour-guidance.md) - Replace pn.fix reads with ApplicationState in CContour.cs
4. [task4-remove-legacy-ui-guards.md](task4-remove-legacy-ui-guards.md) - Remove backend connection guards in GUI.Designer.cs
5. [task5-delete-cnmea-adapter.md](task5-delete-cnmea-adapter.md) - Delete UpdateFromBackendState() method and adapter call
6. [task6-decide-cnmea-fate.md](task6-decide-cnmea-fate.md) - Either delete CNMEA class entirely or keep minimal stub
7. [task7-update-documentation.md](task7-update-documentation.md) - Update docs to reflect CNMEA removal and frontend state

## Success Criteria

**CSim Removal**:
- [ ] CSim.cs deleted from SourceCode/GPS/Classes/
- [ ] All CSim references removed from FormGPS
- [ ] Backend simulator is only GPS data source
- [ ] Development/testing works with backend simulator only

**Position.UpdateFixPosition() Refactoring**:
- [ ] No reads from `pn.speed`, `pn.vtgSpeed`, `pn.hdop`
- [ ] Reads from `_cachedState.Gnss.*` instead
- [ ] Position calculations work correctly
- [ ] No runtime errors or null reference exceptions

**CContour Refactoring**:
- [ ] No reads from `pn.fix.easting/northing`
- [ ] Uses ApplicationState.Gnss.LocalPosition instead
- [ ] Guidance calculations produce correct results
- [ ] Contour following works as before

**Legacy Guard Removal**:
- [ ] GUI.Designer.cs has no `if (_backendClient?.IsConnected != true)` checks
- [ ] UI updates unconditionally from ApplicationState
- [ ] No flickering or cross-thread exceptions
- [ ] UI remains responsive

**CNMEA Adapter Deletion**:
- [ ] UpdateFromBackendState() method removed
- [ ] FormGPS.cs no longer calls pn.UpdateFromBackendState()
- [ ] No compilation errors
- [ ] Application builds successfully

**CNMEA Class Decision**:
- [ ] Either: CNMEA.cs deleted entirely
- [ ] Or: CNMEA.cs reduced to minimal domain stub (if needed for other logic)
- [ ] No unused code remaining
- [ ] All references cleaned up

**Documentation**:
- [ ] docs/implementation sections updated (system architecture, frontend integration)
- [ ] ADR-0002 marked as superseded or updated to reflect removal
- [ ] CLAUDE.md updated to reflect CNMEA removal
- [ ] docs/README.md updated with workflow 005 status

**Overall**:
- [ ] Application builds without errors
- [ ] Backend → Frontend state flow works
- [ ] GPS data displays correctly in UI
- [ ] No runtime crashes or exceptions
- [ ] Performance unchanged (smooth 10 Hz updates)
- [ ] Clear view of remaining frontend logic for future migrations

## Risk Assessment

**Low Risk**:
- CSim removal (backend simulator is proven replacement)
- Legacy guard removal (already tested with backend connected)
- CNMEA adapter deletion (simple method removal)

**Medium Risk**:
- Position.UpdateFixPosition() refactoring (360+ lines, complex logic)
- CContour guidance refactoring (critical for path following)
- Threading issues if ApplicationState access not properly synchronized

**Mitigation**:
- Test each task incrementally
- Keep git commits small and focused
- Run application after each change
- Verify GPS display and guidance before proceeding

## Architecture Context

**Before (Workflow 002 with Adapter)**:
```
Backend (GnssService)
  → SignalR
  → FormGPS.OnStateReceived(ApplicationState state)
     → _cachedState = state
     → pn.UpdateFromBackendState(state)  ← ADAPTER
        → pn.fix.easting = state.Gnss.LocalPosition.Easting
        → pn.speed = state.Gnss.Speed.KilometersPerHour
        → ... (8 more mappings)
     → Position.UpdateFixPosition() reads pn.*
     → CContour reads pn.fix.*
```

**After (This Workflow - Direct Access)**:
```
Backend (GnssService)
  → SignalR
  → FormGPS.OnStateReceived(ApplicationState state)
     → _cachedState = state
     → Position.UpdateFixPosition() reads _cachedState.Gnss.*
     → CContour reads _cachedState.Gnss.LocalPosition.*
```

**Key Change**: Remove intermediate adapter, use ApplicationState directly everywhere.

## Dependencies

**Requires**:
- ✅ Workflow 001 complete (Backend foundation, SignalR)
- ✅ Workflow 002 complete (GPS/GNSS migration, CNMEA adapter)
- ✅ Backend simulator fully functional (SimulatorService, VehiclePhysicsService)

**Enables**:
- Workflow 006+ (Section Control, Guidance, Field migrations)
- Clear frontend/backend boundary
- Future web frontend (Electron + React)

## File Paths

**Files to Modify**:
- [FormGPS.cs](../../SourceCode/GPS/Forms/FormGPS.cs) - Remove adapter call (line 607)
- [Position.Designer.cs](../../SourceCode/GPS/Forms/Position.Designer.cs) - Refactor UpdateFixPosition() (lines 128-363)
- [CContour.cs](../../SourceCode/GPS/Classes/CContour.cs) - Refactor guidance calc (line 684)
- [GUI.Designer.cs](../../SourceCode/GPS/Forms/GUI.Designer.cs) - Remove legacy guards (lines 262-368)
- [CNMEA.cs](../../SourceCode/GPS/Classes/CNMEA.cs) - Delete adapter method (lines 37-60) or entire file

**Files to Delete**:
- [CSim.cs](../../SourceCode/GPS/Classes/CSim.cs) - Frontend simulator (entire file)

**Documentation to Update**:
- [docs/implementation/sections/system-architecture.md](../../implementation/sections/system-architecture.md)
- [docs/implementation/sections/frontend-integration.md](../../implementation/sections/frontend-integration.md)
- [docs/implementation/adrs/adr-0002-frontend-adapter-pattern.md](../../implementation/adrs/adr-0002-frontend-adapter-pattern.md)
- [docs/README.md](../../README.md)
- [CLAUDE.md](../../../CLAUDE.md)

## Next Steps After This Workflow

With CNMEA removed and frontend GPS processing eliminated, the next logical migrations are:

1. **Section Control** (500+ lines, 1 class) - Quick win, high value
2. **Guidance & Path Planning** (4,700+ lines, 7 classes) - Core steering logic
3. **Field Management & Boundaries** (1,800+ lines, 5 classes) - Coverage tracking
4. **Vehicle Configuration** (600+ lines, 3 classes) - Settings sync

See the exploration results in the conversation history for detailed domain analysis.
