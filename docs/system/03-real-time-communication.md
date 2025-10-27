# Real-Time Communication

## Bidirectional Communication Pattern

The system uses **bidirectional real-time communication** between backend and frontend:

- **Backend → Frontend**: State updates pushed automatically (no polling)
- **Frontend → Backend**: Commands sent on-demand (user actions)

This pattern enables low-latency, efficient communication for precision agriculture applications where timing matters.

## Communication Technology

### SignalR
The system uses **ASP.NET Core SignalR** for real-time bidirectional communication.

**Why SignalR?**
- **Push model**: Server pushes updates to clients (no polling overhead)
- **Bi-directional**: Supports both state updates and command dispatch
- **Transport negotiation**: WebSocket preferred, fallback to Server-Sent Events or Long Polling
- **Hub abstraction**: Simple RPC-style API over persistent connections
- **Built-in**: Native ASP.NET Core integration, no additional servers

### Connection Management
- **Persistent connection**: Long-lived TCP connection between client and backend
- **Automatic reconnection**: SignalR handles connection failures and reconnection
- **Connection lifecycle**: OnConnected → Active → OnDisconnected events
- **Multiple clients**: Backend tracks each client connection independently

## State Updates (Backend → Frontend)

### Update Flow
1. **GPS data arrives** at backend (via UDP)
2. **ApplicationOrchestrator processes** data (unpacks, transforms, computes state)
3. **State publisher broadcasts** to SignalR hub
4. **SignalR hub fans out** to all connected clients
5. **Clients receive** state update callback (push notification)
6. **Frontend updates** UI with new state

### Update Rate
- **Variable rate**: Matches GPS arrival rate (typically 5-10 Hz)
- **No polling**: Clients passively receive updates
- **Efficient**: Only send when data changes
- **Low latency**: Push notification (<5ms after backend processes)

### State Content
Each update contains:
- **Timestamp**: When backend processed this state (UTC)
- **GNSS data**: Complete GPS information
  - Geographic position (latitude, longitude)
  - Local coordinates (X, Y, Z meters)
  - Heading (degrees, 0-360)
  - Speed (km/h)
  - Altitude (meters above sea level)
  - Quality metrics (fix type, satellite count, HDOP, age)
  - Health status (active, quality level)

### Broadcast Semantics
- **Fan-out**: Single computation, multiple recipients
- **Concurrent**: All clients receive simultaneously
- **Unacknowledged**: Fire-and-forget (no delivery guarantee)
- **Best-effort**: Network issues may drop updates

## Commands (Frontend → Backend)

### Command Flow
1. **User action** in frontend (button click, setting change)
2. **Frontend creates** command object (type-safe, validated)
3. **Frontend sends** command via SignalR hub method
4. **Backend receives** command in hub
5. **Backend routes** to command handler (via MediatR)
6. **Handler processes** command (changes state, triggers actions)
7. **State change propagates** back via normal state update flow

### Command Types
Currently implemented (simulator control):
- **Start**: Begin simulator with initial position, heading, speed
- **Stop**: Pause simulator
- **Speed commands**: Adjust, Set (instant/smooth), Zero
- **Steering commands**: Set angle, Reset to center
- **Direction commands**: Reverse heading (180° turn)
- **Position commands**: Reset to start location
- **Full reset**: Reset all simulator state

### Command Pattern
- **Type-safe**: Strongly typed command objects (not strings or dictionaries)
- **Validated**: Commands validated before processing
- **Asynchronous**: Backend processes without blocking client
- **Idempotent**: Safe to retry (no duplicate side effects)
- **No response**: Commands don't return values (state updates provide feedback)

## Transport Abstraction

The system uses **transport abstraction** to decouple communication from specific technology:

### Backend Abstraction
```
IStatePublisher interface
  └─ SignalRStatePublisher (current)
  └─ WebSocketPublisher (future)
  └─ gRPCPublisher (future)
```

Backend code uses `IStatePublisher`, not SignalR directly. This enables swapping transports without changing business logic.

### Client Abstraction
```
IBackendClient interface
  └─ SignalRBackendClient (current)
  └─ WebSocketClient (future)
  └─ gRPCClient (future)
```

Client code uses `IBackendClient`, not SignalR client directly. Enables different client implementations.

### Benefits
- **Future-proof**: Easy to add WebSocket, gRPC, or other transports
- **Testing**: Mock implementations for unit tests
- **Platform flexibility**: Different transports for different platforms
- **Performance tuning**: Swap for optimized transport without code changes

## Connection Lifecycle

### Client Connection Sequence
1. **Create client** via factory (with connection options)
2. **Subscribe to state** (register callback for updates)
3. **Connect** to backend (SignalR connection established)
4. **Backend logs**: "Client connected: {ConnectionId}"
5. **Ready**: Client receives state updates, can send commands

### Active Connection
- **State updates flow** automatically (push model)
- **Commands sent** on-demand (user actions)
- **Heartbeat**: SignalR sends keep-alive pings
- **Reconnection**: Automatic on network issues

### Client Disconnection Sequence
1. **Client disconnect** (close application, network failure, explicit disconnect)
2. **SignalR detects** disconnect
3. **Backend logs**: "Client disconnected: {ConnectionId}"
4. **Cleanup**: Backend removes client from broadcast list

### Abnormal Disconnection
If client crashes or network fails:
- **Backend detects** after timeout (typically 30s)
- **Logs error** if exception occurred
- **Continues running** (resilient to client failures)
- **Client can reconnect** (new connection ID)

## Error Handling

### Backend Broadcast Errors
If broadcast fails:
- **Log warning**: Record which client failed
- **Continue**: Don't block other clients
- **Drop connection**: Failed client removed from list

### Client Receive Errors
If client can't process state update:
- **Client logs error**: Local error handling
- **Connection intact**: Still receives next update
- **No backend impact**: Backend unaware of client processing errors

### Command Errors
If command processing fails:
- **Backend logs error**: Record command failure
- **No retry**: Commands are one-shot
- **State unaffected**: Invalid commands don't change state
- **Client sees no change**: State updates reflect actual state

## Connection Configuration

### Backend Configuration
- **Endpoint**: `/statehub` on `http://localhost:5000`
- **CORS**: Allow localhost (for development)
- **JSON serialization**: camelCase properties
- **Timeout**: Default SignalR timeout (30s keep-alive)

### Client Configuration
- **Server URL**: `http://localhost:5000`
- **Hub path**: `/statehub`
- **Reconnection**: Automatic with exponential backoff
- **Logging**: Configurable (Information level default)

## SignalR Hub Methods

The SignalR hub exposes methods for different purposes:

### State Broadcasting (Backend → Client)
- **Method**: `ReceiveState(ApplicationState state)`
- **Direction**: Backend pushes to clients
- **Frequency**: Variable (GPS arrival rate)
- **Content**: Complete application state

### Command Handling (Client → Backend)
- **Method**: `UpdateSimulator(UpdateSimulatorCommand command)`
- **Direction**: Client invokes on backend
- **Frequency**: On-demand (user actions)
- **Content**: Type-safe command object

**Note**: SignalR limitation requires specific methods per command type (generic methods not supported). Current implementation uses unified command with event payload.

## Performance Characteristics

### Latency
- **State update latency**: <5ms (backend compute → client receive)
- **Command latency**: <10ms (client send → backend process)
- **Round-trip**: ~15ms (command → state update reflects change)

### Throughput
- **State updates**: 5-10 Hz (GPS-driven, typically ~60-100 updates/sec)
- **Commands**: On-demand (typically <1/sec, burst up to ~10/sec)
- **Bandwidth**: ~1-2 KB per state update

### Scalability
- **Clients supported**: Currently single client (FormGPS), designed for multiple
- **Backend load**: Minimal (<1% CPU for SignalR broadcasting)
- **Memory**: ~1 MB per connected client

## Security Considerations

### Current Implementation (Development)
- **No authentication**: Open connections (localhost only)
- **No authorization**: All clients have full access
- **No encryption**: Plain TCP (localhost trusted)

### Future (Production)
- **Authentication**: JWT tokens or API keys
- **Authorization**: Role-based command access
- **Encryption**: TLS/HTTPS required
- **Rate limiting**: Prevent command flooding

## Future Enhancements

### Selective Subscriptions
Allow clients to subscribe to specific state subsets:
- GPS-only clients (don't need guidance data)
- Monitoring clients (read-only, no commands)
- Bandwidth optimization (send only requested data)

### State Deltas
Send only changed properties (not full state):
- Reduce bandwidth (especially for remote clients)
- Maintain eventual consistency
- Compression for low-bandwidth scenarios

### Command Acknowledgments
Optional command responses:
- Success/failure indication
- Validation errors returned
- Asynchronous completion notification

### Multiple Backends
Federated backend architecture:
- GPS backend (positioning)
- Guidance backend (path planning)
- Section control backend (implement control)
- Clients subscribe to multiple backends

## Related Documentation

- **[01-system-overview.md](01-system-overview.md)** - Two-process architecture
- **[02-main-processing-loop.md](02-main-processing-loop.md)** - How state updates are generated
- **[07-command-handling.md](07-command-handling.md)** - Command processing details
- **[08-client-integration.md](08-client-integration.md)** - How to connect clients
- **[adr/003-bidirectional-communication.md](adr/003-bidirectional-communication.md)** - Why bidirectional
- **[adr/008-transport-abstraction.md](adr/008-transport-abstraction.md)** - Why abstraction layer
