# ADR 004: Command/Query Separation (CQRS)

## Status
**Accepted**

## Context

The system needs to distinguish between:
- **Queries**: Read state (get GPS position, vehicle status, etc.)
- **Commands**: Change state (start simulator, set speed, enable guidance, etc.)

Traditional approach uses **REST CRUD**:
- GET /api/simulator (query)
- PUT /api/simulator (update)
- POST /api/simulator/start (command)
- Blurs line between query and command

**Problems with REST CRUD**:
- Ambiguous intent (is PUT a query or command?)
- No type safety (HTTP methods are generic)
- Response confusion (commands return data? queries change state?)

## Decision

**Separate commands from queries using CQRS pattern**. Queries are state subscriptions (push). Commands are strongly-typed objects dispatched via MediatR.

## Options Considered

### Option 1: REST CRUD
**Description**: Traditional REST with GET (query) and PUT/POST (command)

**Pros**:
- Familiar (industry standard)
- Simple (HTTP methods)
- Universal (works everywhere)

**Cons**:
- Ambiguous (PUT vs POST vs PATCH)
- No type safety (generic HTTP body)
- Response confusion (what should PUT return?)
- Polling required (queries via GET requests)

### Option 2: CQRS with MediatR ✅ **CHOSEN**
**Description**: Commands are strongly-typed objects routed via MediatR. Queries are SignalR state subscriptions.

**Pros**:
- Clear separation (command vs query)
- Type safety (strongly-typed command objects)
- No response confusion (commands return void)
- Push queries (no polling)
- Testable (mock command handlers)

**Cons**:
- More abstractions (ICommand, MediatR, handlers)
- Not RESTful (uses RPC pattern)
- Requires MediatR library

### Option 3: GraphQL
**Description**: GraphQL mutations (commands) and queries (state)

**Pros**:
- Clear separation (mutation vs query)
- Strongly-typed (schema)
- Flexible (client specifies fields)

**Cons**:
- Complex (GraphQL server setup)
- Overkill (no complex queries needed)
- Polling required (no native push)

## Rationale

**Clear intent**: Command means "change state" (no ambiguity). Query means "read state" (no side effects).

**Type safety**: Command objects are strongly-typed (compile-time checking). Example: `UpdateSimulatorCommand` not `{ "action": "update", "speed": 10 }`.

**No return confusion**: Commands don't return values (void). State changes propagate via normal state updates. Avoids "what should this return?" questions.

**Testability**: Command handlers are testable (no HTTP mocking). Mock IMediator, assert handler was called.

**SignalR synergy**: Commands via hub methods (RPC), queries via state push (subscription). Natural fit for bi-directional communication.

**MediatR benefits**: Decouples hub from handlers (hub doesn't know about business logic). Open/Closed principle (add new commands without modifying hub).

## Consequences

### Positive
- ✅ **Clear separation**: Command changes state, query reads state (no ambiguity)
- ✅ **Type safety**: Strongly-typed commands (compile-time checking)
- ✅ **No return confusion**: Commands return void (state updates provide feedback)
- ✅ **Testable**: Mock handlers (no HTTP mocking)
- ✅ **Decoupled**: Hub routes to handlers (no business logic in hub)
- ✅ **Extensible**: Add new commands without changing hub

### Negative
- ❌ **More abstractions**: ICommand, IRequestHandler, MediatR (learning curve)
- ❌ **Not RESTful**: Doesn't follow REST conventions (RPC-style)
- ❌ **MediatR dependency**: Additional library required

### Neutral
- ⚪ **Async commands**: Commands are fire-and-forget (no immediate response)
- ⚪ **State feedback**: Changes visible via state updates (not command response)

## Implementation

**Delivered in**: Workflow 002 (GPS/GNSS Migration - Simulator Commands)

**Key components**:
- **ICommand**: Marker interface for commands (implements `IRequest` from MediatR)
- **UpdateSimulatorCommand**: Unified simulator command (11 event types)
- **SimulatorEvent**: Event payload (Start, Stop, SpeedSet, SteeringSet, etc.)
- **UpdateSimulatorCommandHandler**: MediatR handler (processes commands)
- **StateHub.UpdateSimulator**: Hub method (dispatches to MediatR)

**Command flow**:
```
Client → SignalR Hub → MediatR → Handler → Domain Service → State Change → State Broadcast
```

**Commit reference**: See Workflow 002 simulator command implementation

**Date**: Workflow 002 completion (2025)

## Related Decisions

- **[003-bidirectional-communication.md](003-bidirectional-communication.md)**: Why SignalR (enables push queries)
- **[007-udp-communication-pattern.md](007-udp-communication-pattern.md)**: Commands don't return values (state updates provide feedback)

## System Documentation

- **[../07-command-handling.md](../07-command-handling.md)**: How commands are processed
- **[../03-real-time-communication.md](../03-real-time-communication.md)**: Communication patterns

## References

- **Workflow 002 Plan**: [../../workflow/002-gps-gnss-migration/plan.md](../../../workflow/002-gps-gnss-migration/plan.md)
- **CQRS pattern**: https://martinfowler.com/bliki/CQRS.html
- **MediatR library**: https://github.com/jbogard/MediatR
- **Command pattern**: https://en.wikipedia.org/wiki/Command_pattern
