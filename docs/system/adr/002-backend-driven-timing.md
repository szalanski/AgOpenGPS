# ADR 002: Backend-Driven Timing

## Status
**Accepted**

## Context

The system needs to decide who owns **application timing** - when to process data and update state.

**Legacy approach (FormGPS)**:
- UI has timer (tmrWatchdog) firing every 250ms
- Timer callback processes GPS, updates vehicle state, renders UI
- All logic coupled to UI thread and timer

**Problems**:
- UI timer controls business logic (coupling)
- Can't run backend headless (needs UI process)
- Frontend must replicate timing logic
- Testing requires full UI stack

## Decision

**Backend owns application timing**. Frontend is passive receiver of state updates.

## Options Considered

### Option 1: Frontend-Driven (Legacy)
**Description**: Frontend has timer, calls backend on each tick

**Pros**:
- Frontend controls refresh rate (matches display needs)
- Simple backend (just responds to requests)
- Familiar pattern (traditional client/server)

**Cons**:
- Can't run backend headless (no frontend, no processing)
- Frontend must replicate timing logic (multiple frontends = duplication)
- Coupling (business logic timing tied to UI refresh rate)
- Polling overhead (frontend must request updates)

### Option 2: Backend-Driven ✅ **CHOSEN**
**Description**: Backend owns timing, pushes state to frontend automatically

**Pros**:
- Headless backend (runs independently of UI)
- Thin frontend (no timing logic, just rendering)
- Single timing source (backend decides when to process)
- Push model (no frontend polling)

**Cons**:
- Frontend passive (can't control update rate)
- Backend more complex (must manage state broadcasting)
- Requires push communication (SignalR, WebSocket)

### Option 3: Negotiated Timing
**Description**: Frontend requests desired rate, backend adapts

**Pros**:
- Flexible (each frontend gets desired rate)
- Bandwidth optimization (slow clients get fewer updates)

**Cons**:
- Complex (rate negotiation protocol)
- Testing harder (non-deterministic timing)
- Not needed yet (single frontend, same rate)

## Rationale

**Headless backend goal**: Phase 1 goal is headless backend (can run without UI). Backend must own timing to achieve this.

**Thin client architecture**: Frontend should be passive renderer (thin client). Business logic timing belongs in backend, not UI layer.

**Multiple frontends future**: Phase 2 adds web UI (Electron + React). Backend-driven timing prevents duplicating timing logic in each frontend.

**Testing simplification**: Backend integration tests work without UI. Can test timing and processing independently.

**Single source of truth**: Backend decides when to process (based on GPS arrival). Frontend just displays what backend computed.

## Consequences

### Positive
- ✅ **Headless backend**: Can run without FormGPS (testing, server deployments)
- ✅ **Thin frontend**: FormGPS just renders (no timing logic)
- ✅ **Testable**: Backend integration tests work without UI
- ✅ **Future-proof**: Web frontend can connect without replicating timing logic
- ✅ **Single truth**: Backend is authoritative (timing, state computation)

### Negative
- ❌ **Frontend passive**: Can't control update rate (receives whatever backend sends)
- ❌ **Backend complexity**: Must manage state broadcasting timing
- ❌ **Push required**: Need SignalR or similar (not simple REST)

### Neutral
- ⚪ **Legacy timer deleted**: tmrWatchdog removed from FormGPS (Workflow 001 change, later reconsidered in Workflow 002 for UI-only updates)
- ⚪ **Variable rate**: Frontend receives updates at GPS rate (5-10 Hz variable, not fixed 4 Hz)

## Implementation

**Delivered in**: Workflow 001 (Backend State Foundation)

**Key changes**:
- ApplicationOrchestrator runs as BackgroundService (independent of frontend)
- ApplicationOrchestrator broadcasts state automatically (not on frontend request)
- FormGPS connects and subscribes (receives state, doesn't request)
- tmrWatchdog timer originally deleted (Workflow 001), later restored for UI-only updates (Workflow 002 plan)

**Note**: Workflow 002 plan proposed restoring tmrWatchdog for UI updates only (labels, colors). Backend still drives GPS processing timing.

**Commit reference**: See Workflow 001 task 3 (ApplicationOrchestrator as hosted service)

**Date**: Workflow 001 completion (2025)

## Related Decisions

- **[001-event-driven-architecture.md](001-event-driven-architecture.md)**: How backend decides when to process (GPS-driven)
- **[003-bidirectional-communication.md](003-bidirectional-communication.md)**: How backend pushes state (SignalR)

## System Documentation

- **[../01-system-overview.md](../01-system-overview.md)**: Two-process architecture (backend owns timing)
- **[../02-main-processing-loop.md](../02-main-processing-loop.md)**: How backend controls processing timing

## References

- **Workflow 001 Plan**: [../../workflow/001-backend-state-foundation/plan.md](../../../workflow/001-backend-state-foundation/plan.md)
- **Thin client pattern**: Backend owns logic, frontend renders only
- **CQRS influence**: Backend owns commands and state (frontend is query client)
