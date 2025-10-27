# Architecture Decision Records (ADRs)

## Purpose

This directory contains **Architecture Decision Records** - documents that capture important architectural decisions made during backend system development, including the context, rationale, and consequences.

## What is an ADR?

An ADR documents:
- **Decision**: What was decided
- **Context**: Why the decision was needed
- **Options**: Alternatives considered
- **Rationale**: Why this option was chosen
- **Consequences**: Trade-offs and implications
- **Delivered**: When/where it was implemented

## Why ADRs?

### Historical Context
Understand WHY decisions were made (not just WHAT was implemented):
- **New team members**: Learn rationale without asking original developers
- **Future changes**: Understand trade-offs before modifying design
- **Avoid repetition**: Don't revisit already-decided questions

### Decision Log
Track key architectural choices:
- **Not all decisions**: Only significant architectural choices (not trivial implementation details)
- **Immutable**: ADRs are historical records (never modified, only superseded)
- **Timestamped**: Know when decision was made (context may change over time)

### Workflow References
ADRs link to workflows where decisions were implemented:
- **Workflow 001**: Backend state foundation decisions
- **Workflow 002**: GPS/GNSS migration decisions
- **Workflow 003**: Simulator domain refactoring decisions

## ADR Index

### 001 - Event-Driven Architecture
**Decision**: ApplicationOrchestrator is GPS-driven (not timer-based)

**Context**: Backend main loop needs to process GPS data and broadcast state. Traditional approach uses fixed-rate timer (4 Hz), but GPS data arrives at variable rate (5-10 Hz).

**Chosen**: Event-driven architecture where GPS packet arrival triggers processing.

**Trade-offs**: Variable rate (matches GPS reality) vs predictable timing (fixed rate).

**Delivered in**: Workflow 002 (GPS/GNSS Migration)

---

### 002 - Backend-Driven Timing
**Decision**: Backend owns application timing (frontend is passive receiver)

**Context**: Need to separate UI rendering from business logic timing. Legacy FormGPS used UI timer (tmrWatchdog at 4 Hz) to drive both processing and rendering.

**Chosen**: Backend drives timing, frontend passively receives state updates.

**Trade-offs**: Headless backend capability vs frontend timer control.

**Delivered in**: Workflow 001 (Backend State Foundation)

---

### 003 - Bidirectional Communication
**Decision**: Use real-time bidirectional communication (not request/response)

**Context**: Need low-latency state updates and command dispatch. Traditional REST API uses request/response (polling).

**Chosen**: SignalR for push-based state updates and RPC-style commands.

**Trade-offs**: Real-time push (low latency) vs simple HTTP REST (familiar pattern).

**Delivered in**: Workflow 001 (Backend State Foundation)

---

### 004 - Command/Query Separation (CQRS)
**Decision**: Separate commands (change state) from queries (read state)

**Context**: Need clear distinction between user actions (commands) and data retrieval (state). RESTful CRUD blurs this line (PUT/POST/GET on same resource).

**Chosen**: CQRS pattern with MediatR for command routing.

**Trade-offs**: Clear separation (command vs query) vs simpler REST CRUD.

**Delivered in**: Workflow 002 (GPS/GNSS Migration - Simulator Commands)

---

### 005 - Simulator Domain Separation
**Decision**: Extract simulator into 3 bounded contexts (Vehicle Physics, GNSS Generation, Protocol Serialization)

**Context**: SimulatorService was monolithic with mixed concerns (physics + GPS + protocol encoding).

**Chosen**: DDD refactoring with 3 domain services (clear boundaries).

**Trade-offs**: Clear separation (maintainability) vs more classes (complexity).

**Delivered in**: Workflow 003 (Simulator Domain Model Refactoring)

---

### 006 - Value Object Modeling
**Decision**: Use value objects for domain primitives (Position, Heading, Speed, Steering)

**Context**: GPS data represented as primitive doubles (lat, lon, heading, speed) - no type safety, invalid states possible (lat=999).

**Chosen**: Value objects with invariant enforcement (invalid states impossible).

**Trade-offs**: Type safety (compile-time checks) vs more types (verbose).

**Delivered in**: Workflow 002-003 (GPS/GNSS Migration + Simulator Refactoring)

---

### 007 - UDP Communication Pattern
**Decision**: Simulator sends UDP packets to backend (not in-memory method calls)

**Context**: Need to test full GPS pipeline (UDP reception, protocol unpacking, processing). Simulator could directly call GnssService (bypass network layer).

**Chosen**: Simulator sends UDP packets (mimics AgIO behavior).

**Trade-offs**: Tests full pipeline (realistic) vs network overhead (loopback latency).

**Delivered in**: Workflow 002 (GPS/GNSS Migration)

---

### 008 - Transport Abstraction
**Decision**: Abstract transport layer (IStatePublisher, IBackendClient interfaces)

**Context**: System uses SignalR for communication, but may need different transports (WebSocket, gRPC) in future.

**Chosen**: Interface abstraction (decouple business logic from transport).

**Trade-offs**: Future-proof (easy to swap transports) vs extra abstraction layer.

**Delivered in**: Workflow 001 (Backend State Foundation)

---

## ADR Format

Each ADR follows this structure:

```markdown
# ADR NNN: Decision Title

## Status
Accepted | Superseded | Deprecated

## Context
- What situation triggered this decision?
- What problem needed solving?
- What constraints existed?

## Decision
- What was decided?
- Clear, concise statement

## Options Considered
1. **Option 1**: Description (pros, cons)
2. **Option 2**: Description (pros, cons)
3. **Chosen Option**: Description (why chosen)

## Rationale
- Why this option was chosen
- Key factors in decision
- Expected benefits

## Consequences
### Positive
- Benefits realized
- Problems solved

### Negative
- Trade-offs accepted
- Limitations introduced

### Neutral
- Changes required
- Impact on other components

## Implementation
- **Delivered in**: Workflow NNN (name)
- **Commit**: Commit hash or PR link (if applicable)
- **Date**: When implemented

## Related Decisions
- Links to related ADRs
- Superseded decisions
- Follow-up decisions

## References
- External docs
- Discussions
- Prior art
```

## ADR Lifecycle

### Proposed
- Decision being discussed
- Not yet implemented
- Status: **Proposed**

### Accepted
- Decision approved
- Implemented in codebase
- Status: **Accepted**

### Superseded
- Decision replaced by newer decision
- Historical record preserved
- Status: **Superseded** (link to replacement ADR)

### Deprecated
- Decision no longer relevant (context changed)
- Not superseded (just obsolete)
- Status: **Deprecated**

## When to Write an ADR

### Do Write ADRs For:
- **Architectural patterns**: Event-driven, CQRS, DDD
- **Technology choices**: SignalR, MediatR, UDP vs TCP
- **Design principles**: Value objects, bounded contexts
- **Trade-offs**: Performance vs simplicity, flexibility vs complexity

### Don't Write ADRs For:
- **Trivial choices**: Variable naming, file organization
- **Temporary decisions**: Placeholder implementations
- **Implementation details**: Which loop to use, algorithm specifics
- **Obvious choices**: Industry-standard practices with no alternatives

## Related Documentation

- **[../README.md](../README.md)** - System documentation index
- **[../../workflow/](../../workflow/)** - Workflow documentation (where ADRs were implemented)
- **[../../architecture/](../../architecture/)** - Theoretical architecture (before implementation)

## Contributing

When making significant architectural decisions:
1. Create new ADR in this directory
2. Use next sequential number (009, 010, etc.)
3. Follow ADR format template above
4. Link to relevant workflow or commit
5. Update this README index
6. Keep ADRs focused (one decision per ADR)
7. Preserve historical ADRs (never delete or modify, only supersede)
