# ADR 0001: GNSS-Orchestrated Backend Pipeline

- Date: 2025-10-28
- Status: Accepted

## Context

Workflow `002-gps-gnss-migration` migrated GNSS processing from the legacy FormGPS timer loop into the API backend. Current code shows the backend is responsible for receiving AgIO UDP packets, deriving GNSS state, and broadcasting application state over SignalR (`SourceCode/AgOpenGPS.Api/Program.cs:34`, `SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs:30`, `SourceCode/AgOpenGPS.Api/Services/GnssService.cs:33`).

## Decision

The backend remains the canonical processor for GNSS data:
- `ApplicationOrchestrator` consumes UDP packets as they arrive and orchestrates state updates.
- `GnssService` performs all PGN 0xD6 unpacking, coordinate transforms, and health computations.
- `SignalRStatePublisher` is the single broadcaster of `ApplicationState` to connected clients.
- The simulator continues to feed the same UDP pipeline, ensuring real and simulated data share identical processing.

## Consequences

- FormGPS integrations must treat the API backend as the source of GNSS truth; any fallback logic must account for backend-driven state.
- Future GNSS enhancements (e.g., IMU handling, configurable origins) extend this pipeline rather than moving logic back to the frontend.
- Implementation docs in `docs/implementation/` must be kept current whenever this pipeline changes, and any significant divergence warrants a new ADR.
