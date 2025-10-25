# Task 4: Create IStatePublisher and SignalRStatePublisher Implementation

## Goal

Create the IStatePublisher abstraction interface and SignalRStatePublisher implementation that broadcasts strongly-typed ApplicationState updates to all connected clients via SignalR.

## Steps

1. Create folder structure: Abstractions/, Services/, Hubs/
2. Create IStatePublisher interface in Abstractions/IStatePublisher.cs
3. Create StateHub class in Hubs/StateHub.cs (inherits from Hub, implementation detail)
4. Create SignalRStatePublisher class in Services/SignalRStatePublisher.cs (implements IStatePublisher)
5. Inject IHubContext<StateHub> into SignalRStatePublisher
6. Implement BroadcastStateAsync method in SignalRStatePublisher
7. Add connection/disconnection logging in StateHub (OnConnectedAsync, OnDisconnectedAsync)
8. Register IStatePublisher in Program.cs dependency injection
9. Register StateHub in Program.cs routing (map hub endpoint /statehub)
10. Configure SignalR options (System.Text.Json serialization, WebSockets transport)
11. Verify hub is accessible at /statehub endpoint

## Key Points

- **IStatePublisher is transport-agnostic** (enables future WebSocket/gRPC implementations)
- **SignalRStatePublisher is the concrete implementation** for SignalR
- StateHub is implementation detail (only used by SignalRStatePublisher)
- ApplicationOrchestrator depends on IStatePublisher, not SignalR types
- BroadcastStateAsync sends strongly-typed ApplicationState to ALL connected clients
- Endpoint: /statehub (lowercase, simple)
- System.Text.Json serialization (default in .NET 8)
- HTTP/WebSockets transport (SignalR auto-negotiates)
- Log client connections for debugging

## Project Structure

After completion, the project should have:

```
AgOpenGPS.Api/
├── Abstractions/
│   └── IStatePublisher.cs           // Transport abstraction
├── Services/
│   ├── ApplicationOrchestrator.cs   // (from Task 3, uses IStatePublisher)
│   └── SignalRStatePublisher.cs     // SignalR implementation
└── Hubs/
    └── StateHub.cs                  // SignalR Hub (implementation detail)
```

## API Design

### IStatePublisher (Abstractions/IStatePublisher.cs)

```csharp
public interface IStatePublisher
{
    Task BroadcastStateAsync(ApplicationState state, CancellationToken ct = default);
}
```

### SignalRStatePublisher (Services/SignalRStatePublisher.cs)

Implements IStatePublisher using SignalR:
- Injects IHubContext<StateHub>
- BroadcastStateAsync calls `hubContext.Clients.All.SendAsync("ReceiveState", state)`
- Logging for broadcast operations
- Error handling for failed broadcasts

### StateHub (Hubs/StateHub.cs)

SignalR Hub (used internally by SignalRStatePublisher):
- Inherits from Hub (minimal implementation)
- OnConnectedAsync - log client connections
- OnDisconnectedAsync - log client disconnections
- No client-to-server methods yet (broadcast-only)

## SignalR Hub Endpoint

After completion, SignalR hub should be accessible at:
- URL: http://localhost:5000/statehub
- Transport: WebSockets (after HTTP upgrade)
- Server→Client method: "ReceiveState" (sends ApplicationState)

## Integration with ApplicationOrchestrator

ApplicationOrchestrator (from Task 3) already uses IStatePublisher:
1. OnTick generates ApplicationState
2. Calls `await _statePublisher.BroadcastStateAsync(state)`
3. SignalRStatePublisher broadcasts via SignalR
4. All connected clients receive state update

## Dependency Injection (Program.cs)

Register services:
```csharp
// SignalR
builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// IStatePublisher implementation
builder.Services.AddSingleton<IStatePublisher, SignalRStatePublisher>();

// Map hub endpoint
app.MapHub<StateHub>("/statehub");
```

## Acceptance

- [ ] Folder structure created (Abstractions/, Services/, Hubs/)
- [ ] IStatePublisher interface exists in Abstractions/IStatePublisher.cs
- [ ] StateHub class exists in Hubs/StateHub.cs
- [ ] SignalRStatePublisher class exists in Services/SignalRStatePublisher.cs
- [ ] SignalRStatePublisher implements IStatePublisher
- [ ] BroadcastStateAsync method implemented (strongly-typed ApplicationState)
- [ ] IHubContext<StateHub> injected into SignalRStatePublisher
- [ ] Connection/disconnection events logged in StateHub
- [ ] IStatePublisher registered in DI (Program.cs)
- [ ] StateHub registered in routing (Program.cs)
- [ ] SignalR configured with System.Text.Json
- [ ] Hub endpoint accessible at /statehub
- [ ] State broadcasts every 100ms to all clients (via ApplicationOrchestrator)

## Test

1. Run AgOpenGPS.Api
2. Logs should show:
   - ApplicationOrchestrator ticks every 100ms
   - SignalRStatePublisher broadcasting state
   - Client connections when connecting
3. Use browser SignalR test tool to connect to http://localhost:5000/statehub
4. Listen for "ReceiveState" method
5. Should receive ApplicationState updates every 100ms with Timestamp
