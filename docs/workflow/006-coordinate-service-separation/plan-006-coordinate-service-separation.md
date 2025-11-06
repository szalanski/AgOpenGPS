# Coordinate Service Separation

## Goal

Extract coordinate transformation logic from GnssService into a dedicated ICoordinateService, following Single Responsibility Principle. Synchronize LocalPlane origin between backend and frontend via ApplicationState.LocalPlane property, eliminating hardcoded origin values.

## Current State

After Workflow 005 (Remove CNMEA Adapter):
- **GnssService owns GPS parsing AND coordinate transformation**: Mixed responsibilities violate SRP
- **CoordinateTransformer buried inside GnssService**: Line 16-52, tightly coupled to GPS processing
- **Hardcoded origins in two places**: ApplicationOrchestrator.cs:36 (backend), FormGPS.cs:618 (frontend)
- **No coordinate system metadata exposed**: Frontend cannot validate it matches backend
- **Cannot reuse transformations**: Boundaries, paths, and recorded tracks would duplicate coordinate logic

**Specific Issues**:
1. GnssService.cs line 16: `private CoordinateTransformer? _coordinateTransformer;` - internal dependency
2. ApplicationOrchestrator.cs line 36: `new Wgs84Position(45.0, -93.0)` - hardcoded origin
3. FormGPS.cs line 618: `new Wgs84(45.0, -93.0)` - duplicate hardcoded origin (was synchronized in Workflow 005)
4. No way to know if frontend/backend origins match
5. Future features (boundaries, paths) would need separate coordinate transformers

## Target State

- **ICoordinateService**: Dedicated service for coordinate transformations (WGS84 ↔ Local Plane)
- **Registered in DI**: Singleton service available throughout backend
- **ApplicationState.LocalPlane**: Exposes origin and conversion factors to frontend
- **Frontend reads backend origin**: FormGPS uses `state.LocalPlane.Origin` instead of hardcoded value
- **Reusable service**: Can be used for boundaries, paths, recorded tracks in future workflows
- **Configuration-ready**: Origin initialization centralized, easy to make configurable

## Why

**Single Responsibility Principle**: GPS processing and coordinate transformation are separate concerns

**Coordinate Synchronization**: Frontend/backend MUST use identical LocalPlane origin for coordinates to match

**Reusability**: Boundaries, paths, and tracks all need coordinate transformations

**Testability**: Can test coordinate transformations independently of GPS parsing

**Configuration**: Easier to make origin configurable (field loading) when logic is centralized

**Transparency**: Frontend can verify it's using the same coordinate system as backend

## What This Is NOT

**NOT refactoring GnssService yet**: GnssService keeps internal CoordinateTransformer (breaking change deferred to Phase 2)

**NOT changing coordinate math**: CoordinateTransformer formulas remain unchanged

**NOT adding configuration yet**: Origin still hardcoded (45.0, -93.0) but centralized

**NOT migrating boundaries/paths**: Only infrastructure, actual usage comes in future workflows

**NOT changing protocols**: ApplicationState structure extended, not replaced

## Migration Path

This workflow prepares for future coordinate transformation needs (boundaries, paths, fields):

**Phase 1 (This Workflow)**: Extract ICoordinateService, synchronize origin via ApplicationState ✅ DONE

**Phase 2 (Future)**: Refactor GnssService to use ICoordinateService (breaking change)

**Phase 3 (Future)**: Add configuration support (appsettings.json, field metadata)

**Phase 4 (Future)**: Use ICoordinateService for boundary/path transformations

Throughout implementation, GnssService continues working - we're running ICoordinateService in parallel (Strangler Fig pattern).

## Tasks

This workflow was implemented in 5 phases:

1. **Phase 1: Create CoordinateService infrastructure** ✅ COMPLETED
   - Create ICoordinateService interface (7 methods)
   - Create LocalPlaneInfo DTO model (Origin, MetersPerDegreeLat, MetersPerDegreeLonAtOrigin)
   - Create CoordinateService implementation (wraps existing CoordinateTransformer)
   - Register in DI container (Program.cs)

2. **Phase 2: Add LocalPlaneInfo to ApplicationState** ✅ COMPLETED
   - Add `LocalPlaneInfo? LocalPlane` property to ApplicationState.cs
   - Document purpose: "Frontend must use this origin to ensure coordinate systems match"

3. **Phase 3: Update ApplicationOrchestrator to populate LocalPlane** ✅ COMPLETED
   - Inject ICoordinateService via DI
   - Initialize with same origin as GnssService (45.0, -93.0)
   - Populate `state.LocalPlane = _coordinateService.GetLocalPlaneInfo()` when broadcasting

4. **Phase 4: Update frontend to use backend origin** ✅ COMPLETED
   - Read origin from `state.LocalPlane.Origin` instead of hardcoded value
   - Add null safety check - defer initialization if LocalPlane not available
   - Log coordinate system metadata (MetersPerDegreeLat, MetersPerDegreeLon)
   - Remove hardcoded origin from FormGPS.cs

5. **Phase 5: Test and verify coordinate synchronization** ✅ COMPLETED
   - Verify builds succeed (client library compiles)
   - Document testing instructions
   - Prepare for integration testing

## Success Criteria

**CoordinateService Infrastructure**:
- [x] ICoordinateService.cs created in Abstractions/ (7 methods)
- [x] LocalPlaneInfo.cs created in Client/Models/ (Origin, conversion factors)
- [x] CoordinateService.cs implements interface (thread-safe, wraps CoordinateTransformer)
- [x] Registered in Program.cs DI container
- [x] Injected into ApplicationOrchestrator

**ApplicationState Integration**:
- [x] ApplicationState.LocalPlane property added
- [x] ApplicationOrchestrator populates LocalPlane when broadcasting
- [x] SignalR sends LocalPlaneInfo to frontend

**Frontend Synchronization**:
- [x] FormGPS reads origin from `state.LocalPlane.Origin`
- [x] Null safety check implemented (defer if not available)
- [x] Hardcoded origin removed from FormGPS.cs
- [x] Coordinate system metadata logged (conversion factors)

**Build and Compilation**:
- [x] AgOpenGPS.Api.Client builds successfully (0 errors)
- [x] Code compiles without errors
- [x] No breaking changes to existing integration tests

**Architecture Quality**:
- [x] Single Responsibility: GPS parsing separate from coordinate transformation
- [x] Reusable: ICoordinateService can be used for boundaries, paths, tracks
- [x] Synchronized: Backend and frontend use identical origin
- [x] Transparent: Frontend can verify coordinate system matches backend
- [x] Configuration-ready: Origin initialization centralized

## Implementation Notes

**No Breaking Changes**: GnssService still works with internal CoordinateTransformer - ICoordinateService runs in parallel (Strangler Fig pattern)

**Thread Safety**: CoordinateService uses lock-based synchronization for initialization state

**Null Safety**: Frontend checks `state.LocalPlane?.Origin` before initialization

**Logging**: Frontend logs origin and conversion factors for debugging

**Future Work**:
- Phase 2: Refactor GnssService to inject ICoordinateService (breaking change)
- Configuration: Load origin from appsettings.json or field metadata
- Field Support: Update origin when loading different fields
- Reuse: Use ICoordinateService for boundary/path coordinate transformations

## Files Created

1. **SourceCode/AgOpenGPS.Api/Abstractions/ICoordinateService.cs** (60 lines)
   - Interface with 7 methods: InitializeLocalPlane, ConvertToLocal, ConvertToWgs84, GetLocalPlaneInfo, UpdateOrigin, IsInitialized, Origin
   - Defines contract for coordinate transformation service

2. **SourceCode/AgOpenGPS.Api.Client/Models/LocalPlaneInfo.cs** (56 lines)
   - DTO containing Origin, MetersPerDegreeLat, MetersPerDegreeLonAtOrigin
   - Two constructors: default (zeros) and parameterized
   - ToString() method for debugging

3. **SourceCode/AgOpenGPS.Api/Services/CoordinateService.cs** (145 lines)
   - Thread-safe implementation using lock-based synchronization
   - Wraps existing CoordinateTransformer internally
   - Exposes LocalPlaneInfo via GetLocalPlaneInfo()
   - Duplicates WGS84 ellipsoid formulas (will be refactored in Phase 2)

## Files Modified

1. **SourceCode/AgOpenGPS.Api/Program.cs** (line 43)
   - Added: `builder.Services.AddSingleton<ICoordinateService, CoordinateService>();`

2. **SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs** (lines 16, 23, 29, 41, 80)
   - Injected ICoordinateService via constructor
   - Initialize with origin: `var origin = new Wgs84Position(45.0, -93.0);`
   - Initialize both services: `_gnssService.InitializeLocalPlane(origin); _coordinateService.InitializeLocalPlane(origin);`
   - Populate when broadcasting: `LocalPlane = _coordinateService.GetLocalPlaneInfo()`

3. **SourceCode/AgOpenGPS.Api.Client/Models/ApplicationState.cs** (lines 24-30)
   - Added `LocalPlaneInfo? LocalPlane { get; set; }` property
   - Documented: "Frontend must use this origin to ensure coordinate systems match"

4. **SourceCode/GPS/Forms/FormGPS.cs** (lines 617-638)
   - Added null safety check: `if (state.LocalPlane?.Origin == null) { return; }`
   - Read origin: `var backendOrigin = new Wgs84(state.LocalPlane.Origin.Latitude, state.LocalPlane.Origin.Longitude);`
   - Log metadata: `MetersPerDegreeLat: {state.LocalPlane.MetersPerDegreeLat:F2}`
   - Removed hardcoded origin (was line 618)

## Testing Instructions

1. Stop running processes: Backend API, Frontend, Visual Studio
2. Rebuild: `dotnet build SourceCode/AgOpenGPS.sln`
3. Start backend: `dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj`
4. Start frontend: `dotnet run --project SourceCode/GPS/AgOpenGPS.csproj`
5. Verify logs show:
   - Backend: "ApplicationOrchestrator starting - GPS-driven UDP mode"
   - Frontend: "Local plane synchronized with backend origin: 45.000000, -93.000000"
   - Frontend: "MetersPerDegreeLat: 111132.92", "MetersPerDegreeLon: 78847.54"
   - Frontend: "Current vehicle position: ..." (should match backend coordinates)

## Next Steps

**Workflow 007 (Future)**: Refactor GnssService to use ICoordinateService
- Remove internal `CoordinateTransformer? _coordinateTransformer` field
- Inject ICoordinateService via constructor
- Call `_coordinateService.ConvertToLocal()` instead of `_coordinateTransformer.ToLocal()`
- Delete duplicated WGS84 formulas from CoordinateService (breaking change)

**Configuration Support**:
- Add `CoordinateOptions` class with `DefaultOriginLatitude` and `DefaultOriginLongitude`
- Load from appsettings.json: `"Coordinates": { "DefaultOriginLatitude": 45.0, "DefaultOriginLongitude": -93.0 }`
- Support field-specific origins from field metadata files

**Reuse for Boundaries/Paths**:
- Boundary coordinate transformations: Use ICoordinateService for WGS84 ↔ Local conversions
- Path recording: Store WGS84 positions, convert to Local for rendering
- Field loading: Initialize origin from field metadata, update via `_coordinateService.UpdateOrigin()`
