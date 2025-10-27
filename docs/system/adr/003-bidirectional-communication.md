# ADR 003: Bidirectional Communication

## Status
**Accepted**

## Context

The system needs communication between backend and frontend with two patterns:
- **Backend → Frontend**: State updates (GPS position, vehicle state, etc.)
- **Frontend → Backend**: Commands (start simulator, set speed, etc.)

Traditional approach uses **HTTP REST**:
- Frontend polls backend (GET /api/state) for state updates
- Frontend sends commands (POST /api/command) when user acts

**Problems with REST**:
- Polling overhead (many requests even if no state changes)
- Latency (updates delayed until next poll)
- Bandwidth waste (full state sent even if one field changed)
- Not real-time (poll interval adds latency)

## Decision

**Use SignalR for real-time bidirectional communication**. Backend pushes state updates, frontend sends commands.

## Options Considered

### Option 1: HTTP REST with Polling
**Description**: Frontend polls backend every 250ms for state, sends commands via POST

**Pros**:
- Simple HTTP (familiar, universal)
- Stateless (no connection management)
- Works everywhere (no WebSocket required)

**Cons**:
- High latency (up to 250ms between polls)
- Bandwidth waste (poll even if no changes)
- Server load (many polls for single state change)
- Not real-time (delayed updates)

### Option 2: WebSocket (Raw)
**Description**: Persistent WebSocket connection, custom message protocol

**Pros**:
- Real-time (push instantly)
- Low latency (<5ms)
- Bi-directional (state updates + commands)
- Efficient (send only when changes)

**Cons**:
- Custom protocol (must design message format)
- More complexity (connection management, reconnection)
- No RPC abstraction (manual serialization)

### Option 3: SignalR ✅ **CHOSEN**
**Description**: ASP.NET Core SignalR (WebSocket with fallback to SSE/Long Polling)

**Pros**:
- Real-time push (like WebSocket)
- RPC abstraction (hub methods, not raw messages)
- Automatic reconnection
- Transport negotiation (WebSocket preferred, fallback to SSE)
- Built-in ASP.NET Core integration
- Bi-directional (state push + command RPC)

**Cons**:
- More dependencies (SignalR library)
- Connection state (not stateless like HTTP)
- Client library required (can't use curl)

### Option 4: gRPC
**Description**: gRPC streaming for state, gRPC calls for commands

**Pros**:
- High performance (HTTP/2, binary)
- Bi-directional streaming
- Strongly-typed (Protobuf schemas)

**Cons**:
- Complex setup (Protobuf definitions)
- Browser support limited (needs gRPC-web proxy)
- Overkill for current needs

## Rationale

**Real-time requirement**: Precision agriculture needs low-latency updates (steering corrections, guidance display). Polling adds unacceptable latency (up to 250ms).

**Push model**: Backend should push state when GPS arrives (not wait for frontend poll). SignalR push is natural fit.

**RPC abstraction**: Hub methods provide clean RPC-style API (not raw message parsing). Example: `await hub.Invoke("UpdateSimulator", command)`.

**ASP.NET Core native**: SignalR is built into ASP.NET Core (no external server). Simple integration.

**Transport flexibility**: SignalR negotiates best transport (WebSocket ideal, SSE fallback). Don't need to handle manually.

**Bi-directional**: State updates and commands use same connection (efficient, simple).

## Consequences

### Positive
- ✅ **Real-time push**: State updates <5ms after backend computes
- ✅ **Low latency**: No polling delay (immediate updates)
- ✅ **Bandwidth efficient**: Only send state when changes (no polling waste)
- ✅ **RPC abstraction**: Clean hub methods (not raw messages)
- ✅ **Auto-reconnection**: SignalR handles connection failures
- ✅ **Bi-directional**: State + commands on same connection

### Negative
- ❌ **Stateful connection**: Must manage connection lifecycle (not stateless REST)
- ❌ **Client library required**: Need SignalR client (can't use simple HTTP client)
- ❌ **Complex debugging**: Connection issues harder to debug than HTTP

### Neutral
- ⚪ **WebSocket preferred**: Uses WebSocket when available (fallback to SSE/Long Polling)
- ⚪ **Connection overhead**: Initial connection setup (~100ms), but persistent after

## Implementation

**Delivered in**: Workflow 001 (Backend State Foundation)

**Key components**:
- **StateHub**: SignalR hub for state broadcasting and command handling
- **SignalRStatePublisher**: IStatePublisher implementation (broadcasts via SignalR)
- **SignalRBackendClient**: IBackendClient implementation (client library)
- **CORS configuration**: Allow localhost connections (development)
- **JSON serialization**: camelCase properties

**Hub methods**:
- `ReceiveState(ApplicationState state)`: Backend → Client (state push)
- `UpdateSimulator(UpdateSimulatorCommand command)`: Client → Backend (command)

**Commit reference**: See Workflow 001 task 4 (StateHub implementation)

**Date**: Workflow 001 completion (2025)

## Related Decisions

- **[002-backend-driven-timing.md](002-backend-driven-timing.md)**: Why backend pushes (not frontend pulls)
- **[004-command-query-separation.md](004-command-query-separation.md)**: Why separate commands from queries
- **[008-transport-abstraction.md](008-transport-abstraction.md)**: Why abstraction layer (IStatePublisher, IBackendClient)

## System Documentation

- **[../03-real-time-communication.md](../03-real-time-communication.md)**: How SignalR communication works
- **[../08-client-integration.md](../08-client-integration.md)**: How clients connect via SignalR

## References

- **Workflow 001 Plan**: [../../workflow/001-backend-state-foundation/plan.md](../../../workflow/001-backend-state-foundation/plan.md)
- **SignalR documentation**: https://docs.microsoft.com/aspnet/core/signalr/
- **Real-time agriculture**: Sub-second latency critical for auto-steer and guidance
