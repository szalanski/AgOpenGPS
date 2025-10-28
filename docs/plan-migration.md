# FormGPS Strangler Migration Plan

## Goal

Complete the Phase&nbsp;1 strangler migration by moving the remaining business logic out of `FormGPS` and its partial classes (`Position.designer.cs`, `OpenGL.Designer.cs`, `Sections.Designer.cs`, `UDPComm.Designer.cs`, etc.) into the backend so the WinForms shell becomes a thin, passive UI. The plan assumes workflows `001-backend-state-foundation` and `002-gps-gnss-migration` (backend infrastructure and GNSS pipeline) are complete except for the pending FormGPS integration step, and that the simulator refactor from workflow `003-simulator-domain-model` is available.

## Guiding Principles

- Preserve the strangler fig pattern: wrap legacy features behind adapters, redirect to the backend, then delete the legacy code once parity is proven.
- Keep FormGPS responsive by using feature flags and state versioning; fall back to legacy paths until backend parity is validated by integration tests.
- Prioritise domains with hard dependencies first (GNSS → Guidance → AutoSteer → Sections/Machine → Field lifecycle).
- Ship vertical slices; each step should leave the application runnable with either backend or legacy logic.
- Leverage the existing simulator + integration tests before turning features on in the UI.

## Migration Order

### Stage 0 – Seal the GNSS Bridge (Pre-requisite)

*Owners: workflow `002-gps-gnss-migration` task 8 and FormGPS team.*

- Replace the temporary logging in `FormGPS.OnStateReceived` with a proper projection layer that updates `AppModel`/`AppViewModel` from `ApplicationState.Gnss`.
- Remove the legacy UDP listeners in `UDPComm.Designer.cs` once the UI reads exclusively from backend GNSS snapshots.
- Wire `IBackendClient` (`BackendClientFactory`) instead of the older `IStateSubscriber` shim so commands and state share the same connection.
- Add health/status UI feedback using `ApplicationState.Gnss.Health` before proceeding to guidance.

### Stage 1 – Introduce a Frontend State Adapter

- Create a thin adapter (e.g., `BackendStateAdapter`) responsible for translating backend snapshots into the legacy model objects (`AppModel`, `tool`, `section[]`, `CTopography`, etc.).
- Route all timer-driven updates (former `ProcessApplicationTick` responsibilities: speed averaging, watchdogs, super-slow detection) through the adapter so WinForms code no longer pulls from raw fields in `Position.designer.cs`.
- Add unit coverage around the adapter mapping and extend integration tests to assert that FormGPS receives full GNSS payloads (latitude, heading, hdop, satellites, fix quality).

### Stage 2 – Guidance Domain Migration

- Extract guidance calculations (AB line selection, curves, tramlines, contour tracking) from `Guidance/` partials (`trk`, `ct`, `curve`, `tram`) into backend services (`GuidanceService`, `GuidanceStateBuilder`).
- Extend `ApplicationState` with a `Guidance` section carrying the active line geometry, look-ahead point (`guidanceLookPos`), cross-track error, and metadata needed by the UI.
- Port supporting value objects (e.g., `vec2`, `GeoDir`, heading smoothing) or provide backend equivalents to avoid double maintenance.
- Add integration tests that stream recorded GNSS paths through the backend and validate guidance outputs before switching the UI to the new state.
- In FormGPS, replace direct calls such as `trk.SnapTrack`/`ct.ResetContour`/`ABLine` manipulations with state-driven rendering and backend commands for line selection.

### Stage 3 – AutoSteer & Vehicle Control

- Inventory AutoSteer dependencies (`guidanceLineDistanceOff`, `guidanceLineSteerAngle`, `yt` YouTurn logic, IMU roll corrections) currently maintained inside `Position.designer.cs`.
- Implement backend command handlers that produce steering commands and broadcast actuator intent. Start with simulator validation, then integrate with the real auto-steer PGNs.
- Expand `ApplicationState` with a `VehicleControl` section (steering enable state, guidance authority, YouTurn status) so the UI can toggle buttons purely off backend state.
- Retire the WinForms-side steering timer and replace button events (`btnAutoSteer`, `btnAutoYouTurn`) with backend intent commands through `IBackendClient.SendCommandAsync`.
- Add safety feature flags to allow falling back to legacy serial outputs until hardware testing passes.

### Stage 4 – Sections, Machine, and Rate Control

- Move section state machines (`section[]`, `btnSection*`, `SendRelaySettingsToMachineModule`, PGN 0x235/0x236/0x252 flows) into a backend `SectionControlService`.
- Have the backend own tool geometry (section widths, overlap) and publish a `SectionState` payload with commanded on/off state, manual overrides, and diagnostics.
- Implement backend mediation for machine module PGNs so FormGPS no longer formats or sends bytes directly; replace UI toggles with idempotent backend commands.
- Ensure the backend handles manual override semantics (auto/manual/off) and logging that currently lives in `JobNew`/`JobClose`.
- Extend integration tests to simulate headland crossings to verify automatic section toggling behaviour.

### Stage 5 – Field Lifecycle and Persistence

- Relocate field/session management from `FormGPS` (`JobNew`, `JobClose`, `ShowSavingFormAndShutdown`, AgShare upload triggers) to backend coordinated services.
- Introduce backend APIs/SignalR commands for field selection, boundary persistence, and AgShare publishing; keep UI limited to surfacing progress/status.
- Provide a backend-side migration path for existing save files so data stays consistent once WinForms stops writing to disk directly.
- Add background synchronization jobs (with retry logic) for uploads the UI currently orchestrates synchronously.

### Stage 6 – UI Shell Flattening and Cleanup

- Strip out remaining computational code from `OpenGL.Designer.cs`, `Sections.Designer.cs`, and related partials so they only render based on backend-provided state.
- Remove obsolete fields and helper methods from `FormGPS` once their responsibilities move to backend services.
- Audit hotkeys, panels, and popups to ensure they call backend commands or read exported state instead of maintaining local copies.
- Finalise telemetry/diagnostics to surface backend connectivity, feature flag status, and fallbacks for operators.

## Cross-Cutting Concerns

- **Testing** – Expand the integration test suite to include regression recordings (GNSS logs, boundary files) before every switchover. Ensure simulator-driven tests cover all new backend services.
- **Feature Flags** – Introduce per-domain flags (e.g., `FeatureFlags.GuidanceFromBackend`, `FeatureFlags.SectionControlFromBackend`) to allow staged rollout and quick rollback.
- **Observability** – Standardise logging and metrics so backend decisions (line selection, section toggles) are traceable from the UI.
- **Documentation** – Update `docs/implementation/sections/*` and `CLAUDE.md` as each stage lands to keep authoritative descriptions aligned with reality.

## Exit Criteria

- FormGPS no longer hosts business logic: all domain decisions are computed in the backend, with the WinForms client acting solely as a presentation layer.
- All operator actions (commands, overrides, field operations) flow through `IBackendClient`.
- Legacy UDP/serial code paths are retired, and hardware integration happens through backend services.
- Integration tests provide coverage for GNSS, guidance, auto-steer, and section behaviours equivalent to or better than the legacy implementation.
- Documentation and observability confirm backend parity, enabling Phase&nbsp;2 (new frontend) without further backend changes.
