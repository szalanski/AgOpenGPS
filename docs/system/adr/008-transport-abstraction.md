# ADR 008: Transport Abstraction

## Status
**Accepted**

## Context

The system uses SignalR for bidirectional communication between backend and frontend. But SignalR is **specific technology choice**, not the only option.

**Future possibilities**:
- Replace SignalR with raw WebSocket (performance optimization)
- Use gRPC streaming (different protocol)
- Support multiple transports (mobile app uses WebSocket, desktop uses SignalR)

**Question**: Should backend code directly use SignalR, or abstract it behind interfaces?

## Decision

**Abstract transport layer behind interfaces**. Backend uses IStatePublisher (not SignalR Hub directly). Client uses IBackendClient (not SignalR client directly).

## Options Considered

### Option 1: Direct SignalR Usage
**Description**: Backend calls `Hub.Clients.All.SendAsync()` directly

**Pros**:
- Simple (no abstraction layer)
- Fewer types (no interfaces)
- Direct access to SignalR features

**Cons**:
- Tight coupling (backend knows SignalR)
- Hard to change transport (SignalR scattered throughout code)
- Hard to test (must mock SignalR Hub)
- Can't support multiple transports

### Option 2: Transport Abstraction ✅ **CHOSEN**
**Description**: Backend uses `IStatePublisher`, client uses `IBackendClient`

**Pros**:
- Decoupled (backend doesn't know SignalR)
- Easy to swap (change implementation, not business logic)
- Testable (mock IStatePublisher)
- Supports multiple transports (factory creates appropriate implementation)

**Cons**:
- Extra abstraction (IStatePublisher, IBackendClient interfaces)
- Indirect (call interface method, not Hub directly)
- Can't use SignalR-specific features (limited to interface)

## Rationale

**Future-proof**: Don't know future transport needs. Abstraction makes swapping transports easy.

**Multiple transports**: Different clients may need different transports:
- **Desktop (FormGPS)**: SignalR (C# client library)
- **Web browser**: SignalR JavaScript library
- **Mobile app**: Native WebSocket (battery-efficient)
- **Embedded device**: gRPC (binary protocol, low bandwidth)

**Testability**: Mock `IStatePublisher` in tests (don't need real SignalR Hub):
```csharp
var mockPublisher = new Mock<IStatePublisher>();
var orchestrator = new ApplicationOrchestrator(..., mockPublisher);
// Test orchestrator without SignalR
```

**Separation of concerns**: Business logic shouldn't know about transport:
- ApplicationOrchestrator shouldn't know SignalR
- GnssService shouldn't know how state is broadcast
- Transport is infrastructure concern (not domain)

**Open/Closed Principle**: Add new transports without modifying backend:
```csharp
// Add WebSocket support
public class WebSocketStatePublisher : IStatePublisher { ... }
// Register in DI, no backend code changes
```

## Consequences

### Positive
- ✅ **Future-proof**: Easy to swap SignalR for WebSocket, gRPC, etc.
- ✅ **Multiple transports**: Different clients can use different transports
- ✅ **Testable**: Mock IStatePublisher (no SignalR in tests)
- ✅ **Decoupled**: Business logic doesn't know transport details
- ✅ **Open/Closed**: Add transports without changing backend

### Negative
- ❌ **Extra abstraction**: IStatePublisher, IBackendClient interfaces (more types)
- ❌ **Indirection**: Call interface (not Hub directly), slight overhead
- ❌ **Limited features**: Interface can't expose every SignalR feature

### Neutral
- ⚪ **SignalR still used**: Current implementation (abstraction doesn't remove SignalR)
- ⚪ **Factory pattern**: BackendClientFactory creates correct implementation

## Implementation

**Delivered in**: Workflow 001 (Backend State Foundation)

**Interfaces defined**:

**Backend (IStatePublisher)**:
```csharp
public interface IStatePublisher
{
    Task BroadcastStateAsync(ApplicationState state);
}
```

**Implementation (SignalRStatePublisher)**:
```csharp
public class SignalRStatePublisher : IStatePublisher
{
    private readonly IHubContext<StateHub> _hubContext;

    public async Task BroadcastStateAsync(ApplicationState state)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveState", state);
    }
}
```

**Client (IBackendClient)**:
```csharp
public interface IBackendClient : IDisposable, IAsyncDisposable
{
    Task ConnectAsync();
    Task DisconnectAsync();
    void SubscribeToState(Action<ApplicationState> onNext, Action<Exception>? onError = null);
    Task SendCommandAsync<TCommand>(TCommand command) where TCommand : ICommand;
    bool IsConnected { get; }
}
```

**Implementation (SignalRBackendClient)**:
- Wraps SignalR HubConnection
- Implements IBackendClient interface
- Hides SignalR details from client code

**Factory**:
```csharp
public static class BackendClientFactory
{
    public static IBackendClient CreateSignalRClient(ConnectionOptions options)
    {
        return new SignalRBackendClient(options);
    }
}
```

**Dependency injection**:
```csharp
// Register in Program.cs
builder.Services.AddSingleton<IStatePublisher, SignalRStatePublisher>();
```

**Usage (backend)**:
```csharp
// ApplicationOrchestrator uses IStatePublisher (not SignalR Hub)
private readonly IStatePublisher _statePublisher;

await _statePublisher.BroadcastStateAsync(state);
```

**Usage (client)**:
```csharp
// FormGPS uses IBackendClient (not SignalR client)
IBackendClient client = BackendClientFactory.CreateSignalRClient(options);
client.SubscribeToState(OnStateReceived);
await client.ConnectAsync();
```

**Commit reference**: See Workflow 001 task 2 (Client library) and task 3 (ApplicationOrchestrator)

**Date**: Workflow 001 completion (2025)

## Future Transport Options

### WebSocket (Raw)
Custom WebSocket implementation (no SignalR overhead):
```csharp
public class WebSocketStatePublisher : IStatePublisher
{
    private readonly WebSocket _webSocket;
    // Custom protocol, lower latency than SignalR
}
```

### gRPC Streaming
gRPC server streaming for state, gRPC calls for commands:
```csharp
public class GrpcStatePublisher : IStatePublisher
{
    private readonly GrpcChannel _channel;
    // Binary protocol, efficient bandwidth
}
```

### Multiple Transports
Support different transports per client:
```csharp
// Factory selects transport based on client type
public static IBackendClient Create(ClientType type)
{
    return type switch
    {
        ClientType.Desktop => new SignalRBackendClient(...),
        ClientType.Web => new WebSocketClient(...),
        ClientType.Mobile => new GrpcClient(...),
    };
}
```

## Related Decisions

- **[003-bidirectional-communication.md](003-bidirectional-communication.md)**: Why SignalR chosen (current implementation)
- **[002-backend-driven-timing.md](002-backend-driven-timing.md)**: Why push model (requires abstracted transport)

## System Documentation

- **[../03-real-time-communication.md](../03-real-time-communication.md)**: Communication patterns (uses abstractions)
- **[../08-client-integration.md](../08-client-integration.md)**: How clients use IBackendClient

## References

- **Workflow 001 Plan**: [../../workflow/001-backend-state-foundation/plan.md](../../../workflow/001-backend-state-foundation/plan.md)
- **Dependency Inversion Principle**: https://en.wikipedia.org/wiki/Dependency_inversion_principle
- **Open/Closed Principle**: https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle
- **Strategy Pattern**: https://en.wikipedia.org/wiki/Strategy_pattern
