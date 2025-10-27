# Client Integration

## How Frontend Applications Connect

Frontend applications (like FormGPS) connect to the backend using a **client library** that abstracts communication details.

## Client Library (AgOpenGPS.Api.Client)

### Purpose
- **Platform**: .NET Standard 2.0 (compatible with .NET Framework 4.8 and .NET 8+)
- **Location**: [SourceCode/AgOpenGPS.Api.Client/](../../SourceCode/AgOpenGPS.Api.Client/)
- **Package**: Reusable library (can be shared across multiple clients)

### What It Provides
- **Backend communication**: SignalR connection management
- **State subscription**: Receive state updates from backend
- **Command dispatch**: Send commands to backend
- **Type safety**: Strongly-typed models and commands
- **Abstraction**: Hide SignalR implementation details

## Client Interface (IBackendClient)

The core abstraction for backend communication:

### Responsibilities
```
IBackendClient
  ├─ ConnectAsync() - Establish connection
  ├─ DisconnectAsync() - Close connection
  ├─ SubscribeToState() - Register callback for state updates
  ├─ SendCommandAsync() - Send commands to backend
  └─ IsConnected - Connection status property
```

### Bidirectional Communication
- **Inbound (Backend → Client)**: State updates via subscription callback
- **Outbound (Client → Backend)**: Commands via SendCommandAsync

### Lifetime Management
- **IDisposable**: Synchronous disposal (legacy compatibility)
- **IAsyncDisposable**: Asynchronous disposal (modern pattern)
- **Cleanup**: Automatically closes connections and releases resources

## Connection Process

### Step 1: Create Client
```
Factory creates client:
  ├─ ConnectionOptions (server URL, logging level)
  ├─ BackendClientFactory.CreateSignalRClient(options)
  └─ Returns IBackendClient instance
```

**Factory pattern**: Hides SignalR-specific creation logic.

### Step 2: Subscribe to State
```
Client subscribes:
  ├─ Provide onNext callback (process state update)
  ├─ Provide onError callback (handle errors)
  └─ Callbacks invoked automatically when state arrives
```

**Observer pattern**: Push notifications (not polling).

### Step 3: Connect
```
await client.ConnectAsync():
  ├─ Establishes SignalR connection to backend
  ├─ Backend logs: "Client connected: {ConnectionId}"
  ├─ Client ready to receive state updates
  └─ Client can send commands
```

### Step 4: Normal Operation
- **Receive states**: onNext callback invoked for each state update (5-10 Hz)
- **Send commands**: Call SendCommandAsync when user acts (on-demand)
- **Error handling**: onError callback if connection issues

### Step 5: Disconnect
```
await client.DisconnectAsync():
  ├─ Closes SignalR connection gracefully
  ├─ Backend logs: "Client disconnected: {ConnectionId}"
  └─ Callbacks stop being invoked
```

## Connection Configuration

### ConnectionOptions
```
ConnectionOptions
  ├─ ServerUrl - Backend address (default: "http://localhost:5000")
  ├─ HubPath - Hub endpoint (default: "/statehub")
  ├─ LogLevel - Logging verbosity (default: Information)
  └─ RetryPolicy - Reconnection behavior (automatic exponential backoff)
```

### Development Configuration
- **Server**: localhost:5000 (same machine)
- **Logging**: Information level (connection events, errors)
- **Retry**: Automatic with exponential backoff

### Production Configuration
- **Server**: Remote backend IP or domain
- **Logging**: Warning level (errors only)
- **TLS**: HTTPS for remote connections
- **Authentication**: JWT tokens (future)

## State Subscription Pattern

### Callback Registration
Client registers two callbacks:

**onNext (State Update)**
```
Called when state arrives:
  ├─ Receives ApplicationState object
  ├─ Client processes state (updates UI, logs, etc.)
  ├─ No return value required (one-way notification)
  └─ Exceptions caught and logged (doesn't crash backend)
```

**onError (Error Handling)**
```
Called on errors:
  ├─ Connection lost
  ├─ Deserialization failures
  ├─ SignalR errors
  └─ Client logs error, may attempt reconnection
```

### Observable Pattern
- **Push model**: Backend pushes, client receives
- **No unsubscribe needed**: Managed by client lifecycle
- **Thread-safe**: Callbacks may be invoked from any thread
- **Buffering**: SignalR buffers if client slow (backpressure)

## Command Dispatch

### Sending Commands
```
await client.SendCommandAsync(command):
  ├─ Command is strongly-typed object
  ├─ Serialized to JSON
  ├─ Sent via SignalR hub method
  ├─ Backend receives and processes
  └─ No response returned (fire-and-forget)
```

### Type Safety
```
Generic method:
  SendCommandAsync<UpdateSimulatorCommand>(command)

Type constraint:
  where T : ICommand (only commands allowed)
```

**Compile-time safety**: Can't send non-command types.

### Asynchronous
Command dispatch is async but:
- **No result**: Commands don't return values
- **Await completes**: When sent to backend (not when processed)
- **State update**: Feedback comes via normal state updates

## FormGPS Integration

### Current Implementation
FormGPS uses the client library:

**Initialization (FormGPS constructor)**
```
1. Create ConnectionOptions (localhost:5000)
2. Call BackendClientFactory.CreateSignalRClient(options)
3. Store client in _backendClient field
4. Subscribe to state: OnStateReceived callback
5. Connect: await _backendClient.ConnectAsync()
6. Log: "Backend connection established"
```

**State Handling (OnStateReceived callback)**
```
1. State update arrives via SignalR
2. OnStateReceived(ApplicationState state) invoked
3. Cache latest state: _latestState = state
4. Update connection status: _isBackendConnected = true
5. (Future): Update UI with state data
```

**Cleanup (FormGPS close)**
```
1. User closes FormGPS
2. FormGPS.OnFormClosing called
3. Dispose backend client: _backendClient?.Dispose()
4. Client disconnects gracefully
5. Backend logs: "Client disconnected"
```

## Error Handling

### Connection Failures
If backend not running when FormGPS starts:
- **ConnectAsync throws**: SocketException or TimeoutException
- **FormGPS catches**: Logs "Backend connection failed"
- **FormGPS continues**: Uses legacy mode (local processing)

### Mid-Operation Disconnection
If backend crashes or network fails:
- **onError callback invoked**: Connection exception passed
- **FormGPS logs error**: "Backend connection lost"
- **AutoReconnect**: SignalR attempts reconnection automatically
- **State freezes**: No new state updates until reconnection

### Command Failures
If command send fails:
- **SendCommandAsync throws**: Connection exception
- **Client logs error**: "Failed to send command"
- **No retry**: Commands are one-shot (user can retry manually)

## Multiple Client Support

### Backend Design
Backend supports multiple simultaneous clients:
- **Broadcast**: All clients receive same state updates
- **Independent commands**: Each client can send commands independently
- **Connection tracking**: Backend logs each client connection ID

### Not Yet Tested
Current focus is single client (FormGPS), but architecture supports:
- **Multiple FormGPS instances**: Different operators viewing same backend
- **Web dashboard**: Browser-based monitoring client
- **Mobile app**: Tablet/phone client for field operators
- **Analytics client**: Data collection and analysis tool

## Client Library Benefits

### Abstraction
Frontend doesn't know about SignalR:
- **Interface**: IBackendClient (not SignalRClient)
- **Swap transport**: Can change to WebSocket, gRPC without changing FormGPS
- **Testing**: Mock IBackendClient for unit tests

### Type Safety
All communication is strongly-typed:
- **ApplicationState**: Structured state (not JSON string)
- **Commands**: Typed objects (not dictionaries)
- **Compile-time checking**: Typos caught by compiler

### Lifetime Management
Client handles connection lifecycle:
- **Connect/Disconnect**: Explicit control
- **Dispose pattern**: Automatic cleanup
- **Reconnection**: Built-in retry logic

### Cross-Platform
.NET Standard 2.0 compatibility:
- **Works in .NET Framework 4.8**: FormGPS can use it
- **Works in .NET 8+**: Future web UI can use it
- **Single library**: Same code for all clients

## Future Client Types

### Web Browser Client
JavaScript/TypeScript client:
- **SignalR JS library**: Microsoft-provided
- **Same hub**: Connects to /statehub
- **Same protocol**: JSON serialization
- **Web UI**: React, Angular, or Vue

### Mobile Client
Xamarin or .NET MAUI app:
- **Reuse client library**: Same AgOpenGPS.Api.Client
- **Mobile-optimized**: Reduced bandwidth mode
- **Offline support**: Cache state for network gaps

### Analytics Client
Background data collector:
- **Read-only**: Only subscribes to state (no commands)
- **High-frequency logging**: Records every state update
- **Database sink**: Writes to time-series database
- **Dashboard**: Grafana or similar visualization

## Client Library Structure

### Models
Domain value objects (shared with backend):
- **ApplicationState**: Top-level state container
- **GnssState**: GPS data
- **Wgs84Position, LocalPosition**: Coordinates
- **Heading, Speed, Altitude**: Measured values
- **GpsQuality, GpsHealth**: Quality indicators

### Commands
Command objects (sent to backend):
- **ICommand**: Marker interface
- **UpdateSimulatorCommand**: Simulator control
- **SimulatorEvent**: Event payload (11 types)

### Abstractions
Transport interfaces:
- **IBackendClient**: Main client interface
- **Factories**: Client creation

### SignalR Implementation
Concrete implementation:
- **SignalRBackendClient**: SignalR-specific client
- **Hub proxy**: SignalR connection wrapper
- **Serialization**: JSON configuration

## Testing with Client Library

### Integration Tests
Backend integration tests use client library:
```
1. Create TestWebApplicationFactory (in-memory backend)
2. Create SignalRBackendClient (connects to test backend)
3. Send commands via client
4. Subscribe to state via client
5. Assert state changes correctly
```

**Tests real client code**: Not mocks (high confidence).

### Unit Tests (FormGPS)
FormGPS can mock IBackendClient:
```
1. Create mock IBackendClient
2. Configure mock responses
3. Inject into FormGPS
4. Test FormGPS logic without backend
```

**Fast tests**: No network, no backend process needed.

## Related Documentation

- **[03-real-time-communication.md](03-real-time-communication.md)** - Communication patterns
- **[07-command-handling.md](07-command-handling.md)** - Command dispatch
- **[09-domain-model.md](09-domain-model.md)** - State models
- **[11-testing-strategy.md](11-testing-strategy.md)** - Integration testing
- **[adr/008-transport-abstraction.md](adr/008-transport-abstraction.md)** - Why IBackendClient interface
