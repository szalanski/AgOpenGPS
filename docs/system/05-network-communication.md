# Network Communication

## UDP-Based Communication

The system uses **UDP (User Datagram Protocol)** for GPS data reception. This choice is intentional and driven by precision agriculture requirements.

## Why UDP Instead of TCP?

### Low Latency
- **No handshake**: UDP sends immediately (no 3-way TCP handshake)
- **No retransmission**: Lost packets aren't retransmitted (reduces latency)
- **No congestion control**: Doesn't slow down when network busy
- **Result**: <1ms network latency (vs 10-50ms for TCP)

### Packet Loss Tolerance
- **GPS is redundant**: New position arrives 5-10 times per second
- **Stale corrections avoided**: Lost packet is replaced by newer data (not retransmitted old data)
- **Better fresh data**: Prefer current GPS position over reliably-delivered stale position
- **Result**: Packet loss doesn't degrade system (next packet corrects)

### Simple Protocol
- **Stateless**: No connection management overhead
- **Broadcast capable**: Single sender can reach multiple receivers
- **Agriculture standard**: AgOpenGPS ecosystem uses UDP
- **Result**: Simple, robust, compatible with existing tools

### Real-Time Priority
Precision agriculture values **timeliness over reliability**:
- **Steering correction**: Needs current position NOW (not reliable old position)
- **Guidance display**: Shows latest data (not queued data)
- **Section control**: Acts on current state (not historical state)

## UDP Listener

Backend runs a UDP listener for GPS data reception:

### Configuration
- **Port**: 15556 (default, configurable)
- **Binding**: All interfaces (0.0.0.0) - accepts from any source
- **Buffer size**: 1024 bytes (handles 128-byte GPS packets easily)

### Listener Lifecycle
1. **Startup**: Opens UDP socket on port 15556
2. **Listening**: Waits for packets asynchronously (non-blocking)
3. **Packet arrival**: Wakes on each packet, yields to application
4. **Shutdown**: Closes socket cleanly on cancellation

### Async Streaming
Uses modern C# async streaming (`IAsyncEnumerable`):
- **Non-blocking**: Thread sleeps when no packets
- **Efficient**: No polling or busy-waiting
- **Backpressure**: Slows down if application can't keep up
- **Cancellation**: Responds to shutdown signals

## Packet Types

The system supports multiple UDP packet types (extensible protocol):

### GPS Packet (Type 0xD6)
- **Purpose**: Position, heading, speed, altitude, quality
- **Size**: 128 bytes (fixed)
- **Rate**: 5-10 Hz (typical GPS)
- **Priority**: Highest (real-time positioning)
- **Status**: Fully implemented

### IMU Packet (Future)
- **Purpose**: Roll, pitch, yaw, accelerations
- **Size**: TBD (likely 64 bytes)
- **Rate**: 50-100 Hz (high-rate orientation)
- **Priority**: High (stability and tilt correction)
- **Status**: Placeholder (not yet implemented)

### AutoSteer Packet (Future)
- **Purpose**: Steering controller feedback, status
- **Size**: TBD (likely 32 bytes)
- **Rate**: Variable (on state change)
- **Priority**: Medium (feedback loop)
- **Status**: Placeholder (not yet implemented)

### Disconnect Packet
- **Purpose**: Graceful disconnection signal
- **Size**: Minimal (header only)
- **Rate**: One-shot (on disconnect)
- **Priority**: Low (cleanup)
- **Status**: Logged (no special handling)

## Packet Routing

ApplicationOrchestrator routes packets by type:

```
UDP packet arrives
  ├─ Check packet header (type identifier)
  ├─ GPS (0xD6) → Process GPS data pipeline
  ├─ IMU → Process orientation data (future)
  ├─ AutoSteer → Process steering feedback (future)
  └─ Disconnect → Log disconnection
```

Each packet type has dedicated processing logic, keeping routing clean and extensible.

## Port Configuration

### Default Ports
- **GPS input**: 15556 (UDP listener)
- **Backend HTTP**: 5000 (SignalR connection)
- **SignalR Hub**: `/statehub` endpoint

### Configuration Sources
Ports configured via:
1. **appsettings.json**: Production configuration
2. **appsettings.Development.json**: Development overrides
3. **Environment variables**: Docker/container deployment
4. **Command-line**: Override at startup

### Port Conflicts
If port 15556 already in use:
- **Backend fails to start**: "Address already in use" exception
- **Check AgIO**: Ensure AgIO isn't running on same port
- **Check simulator**: External simulators may use port
- **Change port**: Configure alternative port if needed

## Network Security

### Current Status (Development)
- **No encryption**: Plain UDP (localhost or trusted LAN)
- **No authentication**: Any source can send packets
- **Port open**: Accepts from any IP address
- **Trust assumed**: AgIO and simulator are trusted sources

### Security Risks
- **Spoofing**: Malicious GPS packets could inject false position
- **Denial of Service**: Flood port with packets (resource exhaustion)
- **Eavesdropping**: Packets visible to network sniffers

### Future Hardening (Production)
- **Source validation**: Accept only from known IPs (whitelist)
- **Packet signing**: HMAC authentication on packets
- **Rate limiting**: Drop excessive packets from single source
- **Encrypted tunnel**: VPN or SSH tunnel for remote GPS

**Note**: For isolated field networks (tractor + backend on private WiFi), current security is adequate.

## Packet Loss Handling

### Detection
- **No explicit ACK**: UDP doesn't confirm delivery
- **Timestamp gaps**: Backend can infer loss from timestamp jumps
- **Age metric**: GPS "age" field indicates staleness

### Impact
- **Minor loss (<5%)**: No visible effect (next packet corrects)
- **Moderate loss (5-20%)**: Occasional jitter in guidance display
- **Severe loss (>20%)**: Degraded guidance quality, user-visible lag

### Mitigation
- **Wired Ethernet**: Preferred for reliability (no WiFi loss)
- **Quality WiFi**: 5GHz, low interference, strong signal
- **Redundant GPS**: Dual GPS receivers for fallback
- **Buffer/predict**: Future enhancement (Kalman filter smoothing)

## Simulator UDP Communication

The built-in simulator **sends UDP packets** to backend (same as AgIO):

### Why Simulator Uses UDP?
- **Tests full pipeline**: Validates UDP reception, unpacking, processing
- **Realistic behavior**: Mimics real GPS source (AgIO)
- **No shortcuts**: Doesn't bypass network layer (finds bugs)

### Simulator Flow
```
Simulator (Backend)
  ├─ Generate GPS data (physics tick every 93ms)
  ├─ Encode binary packet (PGN 0xD6 format)
  ├─ Send via UDP to localhost:15556
  └─ Backend receives (same as external GPS)
       └─ Normal processing pipeline
```

### Loopback Communication
- **Localhost only**: 127.0.0.1 (no external network)
- **Zero latency**: Loopback is instant
- **Always reliable**: Loopback never drops packets
- **Testing-friendly**: Integration tests use same pattern

## Network Topology

### Development Setup
```
┌──────────────────────────────────┐
│  Single Machine (localhost)      │
│                                   │
│  AgIO or Simulator               │
│         │                         │
│         │ UDP 127.0.0.1:15556     │
│         ▼                         │
│  Backend (AgOpenGPS.Api)          │
│         │                         │
│         │ SignalR localhost:5000  │
│         ▼                         │
│  Frontend (FormGPS)               │
└──────────────────────────────────┘
```

### Field Deployment (Future)
```
┌─────────────────┐      UDP WiFi      ┌──────────────────┐
│  Tractor        │                     │  Office PC       │
│  - GPS Receiver │◄────────────────────│  - Backend       │
│  - AgIO         │  Port 15556         │  - SignalR Hub   │
└─────────────────┘                     └──────────────────┘
                                                │
                                                │ SignalR HTTP/HTTPS
                                                │
                                        ┌───────┴─────────┐
                                        │                 │
                                   ┌────▼────┐      ┌────▼────┐
                                   │ Tablet  │      │ Laptop  │
                                   │ Display │      │ Control │
                                   └─────────┘      └─────────┘
```

### Cloud Deployment (Future)
```
Field GPS ──UDP──▶ Backend (Cloud) ──SignalR HTTPS──▶ Web UI (Browser)
```

## Network Performance

### Bandwidth Requirements
- **GPS packets**: ~1 KB @ 10 Hz = 10 KB/sec = 80 Kbps
- **SignalR state**: ~2 KB @ 10 Hz = 20 KB/sec = 160 Kbps
- **Commands**: Negligible (<1 Kbps)
- **Total**: ~250 Kbps (0.25 Mbps)

**Result**: Works on slowest WiFi (1 Mbps+) or 4G cellular

### Latency Requirements
- **GPS → Backend**: <5ms acceptable, <1ms ideal
- **Backend → Frontend**: <10ms acceptable, <5ms ideal
- **Round-trip (command)**: <20ms acceptable, <10ms ideal

**Result**: WiFi sufficient (5-20ms), wired Ethernet ideal (<1ms)

## Monitoring and Diagnostics

### Packet Reception Logging
Backend logs:
- **Packet received**: Debug level (per packet, verbose)
- **Packet processed**: Information level (periodic summary)
- **Packet errors**: Warning level (checksum failures, invalid data)
- **Connection issues**: Error level (socket failures)

### Metrics (Future)
- **Packet rate**: Packets/sec received
- **Packet loss**: Inferred from gaps
- **Processing latency**: Time from receive to broadcast
- **Error rate**: Invalid/dropped packets

### Troubleshooting
- **No packets**: Check AgIO running, port 15556 open, firewall rules
- **Packet loss**: Check WiFi signal, interference, bandwidth
- **High latency**: Check network congestion, CPU usage
- **Invalid packets**: Check AgIO version compatibility, protocol mismatch

## Related Documentation

- **[02-main-processing-loop.md](02-main-processing-loop.md)** - How packets trigger processing
- **[04-gnss-data-pipeline.md](04-gnss-data-pipeline.md)** - GPS packet processing
- **[06-simulator-capabilities.md](06-simulator-capabilities.md)** - Simulator UDP generation
- **[adr/007-udp-communication-pattern.md](adr/007-udp-communication-pattern.md)** - Why UDP for simulator
