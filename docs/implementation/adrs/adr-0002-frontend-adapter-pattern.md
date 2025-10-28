# ADR 0002: Frontend GNSS Integration via Adapter Pattern

- Date: 2025-10-28
- Status: Accepted
- Supersedes: None
- Related: ADR-0001 (GNSS-Orchestrated Backend Pipeline)

## Context

Workflow `002-gps-gnss-migration` Task 8 required integrating FormGPS with the backend GNSS pipeline established in ADR-0001. FormGPS contains hundreds of references to legacy GPS fields (pn.fix, pn.speed, pn.heading, etc.) scattered across UI code, guidance calculations, and rendering logic.

Two integration approaches were considered:

1. **Direct Refactoring** - Replace all legacy field references with ApplicationState property access
2. **Adapter Pattern** - Create a translation layer between backend state and legacy fields

Direct refactoring would require touching hundreds of code locations simultaneously, creating high regression risk and making incremental testing difficult. The codebase is also under active development by multiple teams (MVP pattern refactor in AgOpenGPS.Core, backend migration in cross-platform-support branch), making large-scale UI changes risky.

Current code shows the adapter implementation in SourceCode/GPS/Classes/CNMEA.cs:37-60 (`UpdateFromBackendState` method), connection establishment in SourceCode/GPS/Forms/FormGPS.cs:561-613, and legacy bypass guard in SourceCode/GPS/Forms/UDPComm.Designer.cs:60-66.

## Decision

Implement FormGPS backend integration using the Adapter Pattern with the following components:

1. **CNMEA.UpdateFromBackendState** - Adapter method translates ApplicationState.Gnss properties to legacy CNMEA fields
2. **FormGPS._cachedState** - Stores latest ApplicationState for UI thread access
3. **FormGPS.OnStateReceived** - SignalR callback that caches state and invokes adapter
4. **UDPComm legacy bypass guard** - Disables legacy UDP GPS processing when backend connected
5. **GUI timer guards** - Prevents legacy timers from overwriting backend data

This implements the Strangler Fig migration pattern: the backend replaces legacy GPS processing while maintaining complete compatibility with existing FormGPS code.

## Consequences

### Benefits

**Zero Breaking Changes** - Existing UI code continues to reference legacy fields without modification. All labels, displays, guidance calculations, and OpenGL rendering work transparently through the adapter. This eliminates regression risk during Phase 1 migration.

**Incremental Migration** - The adapter enables gradual migration. Backend GPS processing is replaced immediately, but UI refactoring can proceed at a controlled pace. Each UI component can be updated independently to consume ApplicationState directly.

**Testability** - Adapter logic is isolated in a single 24-line method, simplifying validation. Integration tests verify backend state correctly populates all 10 legacy fields. If adapter bugs occur, they are easy to locate and fix.

**Reversibility** - If backend issues arise, the adapter can be bypassed by disabling the connection guard (setting `_backendClient` to null), instantly restoring legacy UDP processing. No code rollback required.

**Team Coordination** - The adapter isolates backend migration work (cross-platform-support branch) from MVP pattern refactoring (AgOpenGPS.Core). Teams can work independently without coordination overhead.

### Costs

**Code Duplication** - GPS field values exist in both ApplicationState and legacy CNMEA fields during Phase 1. This consumes approximately 2KB memory per update (bounded, as state is replaced on each broadcast). The duplication is intentional for safety.

**Maintenance Burden** - The adapter must be kept in sync with ApplicationState schema changes. If backend adds GPS fields, the adapter needs updating. This cost is acceptable given the reduced regression risk.

**Not Pure DDD** - The adapter couples domain models (ApplicationState) to legacy infrastructure (CNMEA fields). This violates separation of concerns. The compromise enables incremental migration; architectural purity is deferred to Phase 2.

**Performance Overhead**:
- SignalR Latency: Adds approximately 5-10ms compared to local UDP processing. Acceptable for 10 Hz GPS updates.
- Thread Marshaling: OnStateReceived must marshal SignalR callbacks to UI thread via BeginInvoke (FormGPS.cs:589-593). Adds less than 1ms overhead.
- Adapter Execution: 10 field assignments take approximately 0.1ms. Negligible impact on 100ms update cadence.
- Total overhead: Approximately 10ms per update. Guidance calculations remain well within timing budgets.

### Fallback Behavior

When the backend is unavailable, FormGPS gracefully degrades to legacy UDP processing without user intervention:

- Connection guard (UDPComm.Designer.cs:63) evaluates to false when `_backendClient.IsConnected` is false
- Legacy timer guards (GUI.Designer.cs:262-368) allow legacy updates when backend not connected
- No crashes, warnings, or error dialogs - degradation is transparent

This provides seamless failover during backend development and deployment.

### Migration Roadmap

The adapter is a temporary bridge intended for removal in Phase 2:

**Phase 1 (Current - Completed)**
- Backend processes GPS via ApplicationOrchestrator and GnssService
- Adapter translates ApplicationState to legacy fields
- UI code unchanged, references legacy fields
- Legacy UDP processing disabled when backend connected

**Phase 2 (Planned)**
- Refactor FormGPS UI components to consume ApplicationState directly
- Remove adapter method once all legacy references eliminated
- Delete legacy CNMEA fields (fix, speed, heading, etc.)

**Phase 3 (Future)**
- Delete legacy UDP GPS processing code entirely from UDPComm.Designer.cs
- Remove connection guards (no fallback needed)
- CNMEA class either removed or refactored to contain only domain logic

### Removal Criteria

The adapter can be removed when all of the following are true:

1. All FormGPS GPS field references are refactored to use ApplicationState properties
2. Legacy UDP GPS processing code is deleted from UDPComm.Designer.cs
3. CNMEA class is either removed or refactored to contain only domain logic (not adapter mapping)
4. Integration tests updated to verify ApplicationState consumption (not legacy field population)

### Field Mapping Reference

The adapter performs the following translations:

| Backend Property | Legacy Field | Description |
|------------------|--------------|-------------|
| state.Gnss.LocalPosition.Easting | fix.easting | Local plane X coordinate (meters) |
| state.Gnss.LocalPosition.Northing | fix.northing | Local plane Y coordinate (meters) |
| state.Gnss.Speed.KilometersPerHour | speed, vtgSpeed | Ground speed (km/h) |
| state.Gnss.Altitude.Meters | altitude | Elevation above WGS84 ellipsoid (meters) |
| state.Gnss.HeadingSingle.Degrees | headingTrue | True course 0-360 degrees |
| state.Gnss.HeadingDual.Degrees | headingTrueDual | Dual-antenna heading (if available) |
| state.Gnss.Quality.FixQuality | fixQuality | NMEA fix quality 0-8 |
| state.Gnss.Quality.SatellitesTracked | satellitesTracked | Number of satellites in solution |
| state.Gnss.Quality.Hdop | hdop | Horizontal dilution of precision |
| state.Gnss.Quality.Age | age | Age of differential correction (seconds) |

See SourceCode/GPS/Classes/CNMEA.cs:37-60 for the authoritative implementation.

## Related Documentation

- [Frontend Integration Guide](../sections/frontend-integration.md) - Complete usage documentation
- [Communication Protocols](../sections/communication-protocols.md) - IBackendClient interface details
- [Operational Workflows](../sections/operational-workflows.md) - State reception flow
- [System Architecture](../sections/system-architecture.md) - Adapter layer position in system