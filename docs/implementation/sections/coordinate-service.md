# Coordinate Service

## Purpose

The coordinate service centralises WGS84 ⇄ local-plane conversions so every backend component and client UI operate on a consistent field origin. It was introduced to prepare for broader domain migrations (boundaries, guidance, recorded tracks) and to keep GNSS processing focused on packet decoding.

## Backend Responsibilities

- `ICoordinateService` (SourceCode/AgOpenGPS.Api/Abstractions/ICoordinateService.cs:6) defines the contract for:
  - Initialising the local plane (`InitializeLocalPlane`)
  - Querying initialisation state (`IsInitialized`, `Origin`)
  - Converting between WGS84 and local coordinates (`ConvertToLocal`, `ConvertToWgs84`)
  - Inspecting diagnostic metadata (`GetLocalPlaneInfo`)
  - Updating the origin when fields change (`UpdateOrigin`)
- `CoordinateService` (SourceCode/AgOpenGPS.Api/Services/CoordinateService.cs:13) wraps `CoordinateTransformer` behind a thread-safe lock, keeping conversion logic reusable without re-instantiating transformers across services.
- The service is registered as a singleton alongside GNSS services (`SourceCode/AgOpenGPS.Api/Program.cs:43`), so orchestrators, command handlers, and future domain services all share the same origin.

## GNSS Broadcast Flow

- `ApplicationOrchestrator` injects `ICoordinateService` and initialises it together with `IGnssService` when the host starts (`SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs:23`).
- When GNSS packets arrive, the orchestrator embeds the current plane metadata in outgoing state: `LocalPlane = _coordinateService.GetLocalPlaneInfo()` (`SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs:84`).
- `ApplySimulatorCommandHandler` (SourceCode/AgOpenGPS.Api/Commands/Handlers/UpdateSimulatorCommandHandler.cs:24) re-initialises both GNSS and coordinate services whenever the simulator start command carries a new origin. This prevents the “floating map” jitter that occurred when placeholder origins were used.

## Frontend Consumption

- `ApplicationState.LocalPlane` surfaces the origin and conversion factors (`SourceCode/AgOpenGPS.Api.Client/Models/ApplicationState.cs:31`).
- On the first valid GNSS packet, `FormGPS.OnStateReceived` reads the backend origin and calls `CNMEA.DefineLocalPlane` to align the WinForms local plane with the backend (`SourceCode/GPS/Forms/FormGPS.cs:607`).
- If the backend has not yet supplied a plane, the frontend defers initialisation and logs a warning, avoiding mismatched coordinate systems.

## Diagnostics and Testing

- `LocalPlaneInfo` (SourceCode/AgOpenGPS.Api.Client/Models/LocalPlaneInfo.cs:7) records the WGS84 origin and metres-per-degree factors. FormGPS logs this data when synchronisation occurs (`SourceCode/GPS/Forms/FormGPS.cs:626`), making it easy to verify both sides agree on scaling.
- The backend service exposes `GetLocalPlaneInfo()` for health checks or telemetry dashboards; consumers can confirm initialisation without inspecting private state.

## Future Work

- Refactor `GnssService` to consume `ICoordinateService` instead of maintaining its own transformer (planned follow-up).
- Replace the hard-coded default origin with configuration or field metadata once a field-loading workflow exists.
- Extend the service to support multiple named origins if multi-field sessions are introduced.
