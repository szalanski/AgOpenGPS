# Main Processing Loop

## Event-Driven Architecture

The backend system uses an **event-driven architecture** where external GPS data arrival triggers all processing. This fundamentally differs from traditional timer-based approaches.

### Traditional Timer-Based Approach (Legacy)
```
Timer fires every 250ms (4 Hz)
  ├─ Read GPS data if available
  ├─ Process vehicle state
  ├─ Update UI
  └─ Repeat on next timer tick
```

**Problem**: Processing happens even when no new data arrived (wasted cycles), or new data waits for next timer tick (latency).

### Event-Driven Approach (Current Backend)
```
GPS data arrives via UDP
  ├─ Process immediately
  ├─ Compute state
  ├─ Broadcast to clients
  └─ Wait for next GPS packet
```

**Benefit**: Processing happens exactly when needed, minimal latency, variable rate matches GPS reality.

## ApplicationOrchestrator

The **ApplicationOrchestrator** is the backend's main processing loop. It orchestrates data flow through the system.

### Lifecycle

#### Startup
1. System starts ApplicationOrchestrator as background service
2. Initializes coordinate system (local plane origin)
3. Opens UDP listener on port 15556
4. Enters main loop waiting for data
5. Logs: **"ApplicationOrchestrator starting - GPS-driven UDP mode"**

#### Main Loop
The loop continuously processes incoming data:

```
Loop forever (until shutdown):
  ├─ Wait for next UDP packet
  ├─ Packet arrives → Process based on type
  │   ├─ GPS packet → Unpack, transform coordinates, broadcast state
  │   ├─ IMU packet → (Future: Process orientation data)
  │   ├─ AutoSteer packet → (Future: Process steering feedback)
  │   └─ Disconnect packet → Log disconnection
  └─ Return to waiting
```

#### Shutdown
1. Cancellation requested (Ctrl+C or process termination)
2. Loop exits cleanly
3. UDP listener closes
4. Resources disposed
5. Logs: **"ApplicationOrchestrator stopped"**

### Packet Processing

When a packet arrives, ApplicationOrchestrator routes it based on packet type:

#### GPS Packet (Currently Implemented)
1. **Receive**: UDP packet arrives (binary data)
2. **Unpack**: Extract GPS data (latitude, longitude, heading, speed, altitude, quality metrics)
3. **Transform**: Convert WGS84 coordinates → Local plane coordinates
4. **Validate**: Check GPS fix quality and data freshness
5. **State**: Populate application state with GPS data
6. **Broadcast**: Send state to all connected clients via SignalR

#### IMU Packet (Placeholder)
Currently logs: "IMU packet received - not yet implemented"

Future: Will process orientation data (roll, pitch, yaw) for vehicle attitude.

#### AutoSteer Packet (Placeholder)
Currently logs: "AutoSteer packet received - not yet implemented"

Future: Will process steering controller feedback and status.

#### Disconnect Packet
Logs: "Disconnect packet received"

Used for graceful disconnection signaling from external systems.

## Processing Rate

### Variable Rate (GPS-Driven)
The system processes at **GPS arrival rate**, which varies:
- **RTK GPS**: Typically 10 Hz (100ms intervals)
- **Standard GPS**: Typically 5 Hz (200ms intervals)
- **AgIO relay**: Matches source GPS rate
- **Simulator**: Exactly 93ms intervals (≈10.75 Hz)

### No Fixed Timer
Unlike legacy systems, there is **no internal timer** driving the loop. The loop sleeps until data arrives, then wakes and processes immediately.

**Benefits**:
- **Lower latency**: No waiting for next timer tick
- **Efficient**: No processing when no data (CPU sleeps)
- **Realistic**: Matches actual GPS behavior (variable timing)
- **Headless-ready**: Backend doesn't depend on UI refresh rate

## State Broadcasting

After processing each GPS packet, ApplicationOrchestrator broadcasts state:

### State Composition
- **Timestamp**: When backend processed this data (UTC)
- **GNSS State**: Complete GPS information
  - Position (WGS84 latitude/longitude + Local plane X/Y/Z)
  - Heading (degrees)
  - Speed (km/h)
  - Altitude (meters)
  - Quality metrics (fix type, satellite count, HDOP, age)
  - Health status (GPS active, fix quality level)

### Broadcast Mechanism
- Uses SignalR Hub to broadcast to all connected clients
- Clients receive state asynchronously (push model)
- No polling required (clients are notified automatically)
- Rate matches GPS packet arrival (typically 5-10 Hz)

### Multiple Clients
System supports multiple simultaneous clients:
- All clients receive identical state updates
- Broadcast is fan-out (one compute, many receivers)
- Clients connect/disconnect independently
- Backend tracks connection count (logging)

## Error Handling

ApplicationOrchestrator handles errors gracefully:

### Packet Processing Errors
If processing fails:
1. Log error with packet type
2. Continue to next packet (don't crash)
3. Invalid GPS data → Skip broadcast (no state update)
4. Clients receive previous good state (stale but valid)

### UDP Reception Errors
If UDP listener fails:
1. Loop exits
2. ApplicationOrchestrator stops
3. Backend remains running (can restart services)
4. Logged for diagnostics

### Client Communication Errors
If SignalR broadcast fails:
1. Log warning
2. Continue processing next packet
3. Working clients unaffected
4. Failed client connection dropped

## Comparison: Legacy vs Backend-Driven

| Aspect | Legacy (FormGPS) | Backend (ApplicationOrchestrator) |
|--------|------------------|-----------------------------------|
| **Trigger** | Timer (250ms fixed) | GPS data arrival (variable) |
| **Rate** | 4 Hz constant | 5-10 Hz (matches GPS) |
| **Latency** | Up to 250ms delay | Immediate (<5ms) |
| **CPU Usage** | Continuous (even if idle) | Only when data arrives |
| **Location** | FormGPS (UI thread) | Backend (background thread) |
| **Headless** | Requires UI process | Runs independently |
| **Testing** | Needs full UI | Integration tests only |

## Integration with Other Systems

### GPS Data Source (AgIO or Simulator)
- External system sends UDP packets → Port 15556
- ApplicationOrchestrator receives via UDP listener
- Rate controlled by source (not backend)
- Backend is **reactive** (responds to data, doesn't poll)

### SignalR State Publisher
- ApplicationOrchestrator calls state publisher after processing
- State publisher fans out to all connected clients
- Asynchronous operation (doesn't block main loop)
- Failures logged but don't stop processing

### Command Handler (via SignalR Hub)
- Commands arrive from clients → SignalR Hub
- Hub routes to command handlers (MediatR)
- Commands processed independently of main loop
- State changes propagate via normal broadcast cycle

## Threading Model

### Background Service Thread
ApplicationOrchestrator runs on dedicated background thread:
- **Non-blocking**: Doesn't block ASP.NET Core request threads
- **Long-running**: Continuous loop until cancellation
- **Async**: Uses async/await for I/O operations (UDP, SignalR)

### UDP Listener Thread
UDP listener uses async streaming:
- **IAsyncEnumerable**: Yields packets as they arrive
- **Non-blocking**: Thread sleeps when no packets
- **Efficient**: No busy-waiting or polling

### SignalR Hub Threads
SignalR operates on ASP.NET Core thread pool:
- **Concurrent**: Multiple clients handled simultaneously
- **Thread-safe**: SignalR manages synchronization
- **Broadcast**: Uses internal thread pool for fan-out

### Simulator Thread (If Enabled)
Simulator runs on separate timer:
- **Fixed rate**: 93ms timer (physics tick)
- **Sends UDP**: Feeds packets back to main loop
- **Independent**: Doesn't directly call ApplicationOrchestrator

## Future Extensions

The event-driven architecture enables future capabilities:

### Multiple Data Sources
- Process GPS, IMU, steering simultaneously
- Each source triggers independent processing
- Combine results in unified state

### Priority Processing
- Critical packets (GPS) processed immediately
- Non-critical packets (diagnostics) batched
- QoS-aware packet routing

### Dynamic Subscriptions
- Clients subscribe to specific state subsets
- Reduce bandwidth for remote clients
- Filter state updates by relevance

### Event Sourcing
- Record all incoming packets (event log)
- Replay for debugging or analysis
- Time-travel debugging capabilities

## Related Documentation

- **[01-system-overview.md](01-system-overview.md)** - Overall system architecture
- **[04-gnss-data-pipeline.md](04-gnss-data-pipeline.md)** - GPS processing details
- **[05-network-communication.md](05-network-communication.md)** - UDP packet details
- **[03-real-time-communication.md](03-real-time-communication.md)** - SignalR broadcasting
- **[adr/001-event-driven-architecture.md](adr/001-event-driven-architecture.md)** - Why GPS-driven
- **[adr/002-backend-driven-timing.md](adr/002-backend-driven-timing.md)** - Why backend owns timing
