# ADR 007: UDP Communication Pattern (Simulator)

## Status
**Accepted**

## Context

The simulator needs to provide GPS data to the backend. Two approaches:

**Option A: Direct method call**:
```csharp
Simulator generates data → Call GnssService.ProcessGpsData(data)
```

**Option B: Send UDP packet**:
```csharp
Simulator generates data → Encode to binary → Send UDP to localhost:15556
→ Backend receives UDP → Unpacks → GnssService processes
```

**Question**: Should simulator bypass network layer (direct call) or go through full pipeline (UDP)?

## Decision

**Simulator sends UDP packets** (mimics AgIO behavior). Tests full GPS pipeline (UDP reception, binary unpacking, processing, state broadcasting).

## Options Considered

### Option 1: Direct Method Call
**Description**: Simulator calls GnssService directly (in-memory)

**Pros**:
- Faster (no network overhead, even loopback)
- Simple (no UDP code needed)
- No protocol encoding needed

**Cons**:
- Doesn't test UDP reception (bypassed)
- Doesn't test protocol unpacking (no binary encoding)
- Doesn't test realistic data flow (shortcuts network layer)
- Integration tests miss bugs in UDP/protocol code

### Option 2: Send UDP Packets ✅ **CHOSEN**
**Description**: Simulator sends UDP packets to localhost:15556 (same as AgIO)

**Pros**:
- Tests full pipeline (UDP reception + unpacking + processing)
- Realistic (mimics AgIO exactly)
- Finds bugs (network issues, protocol errors caught in tests)
- Integration tests validate complete data flow

**Cons**:
- Slight overhead (loopback UDP, encoding/decoding)
- More code (UDP send, protocol serialization)
- Simulator more complex (needs protocol knowledge)

## Rationale

**Test realism**: Simulator should test what production does. Production receives UDP from AgIO, so simulator should send UDP (not bypass network layer).

**Bug detection**: Integration tests use simulator. If simulator bypasses UDP, tests don't catch:
- UDP port configuration errors
- Protocol unpacking bugs (wrong byte offsets)
- Serialization issues (endianness, scaling)
- Network layer problems

**AgIO compatibility**: Simulator uses same protocol as AgIO (PGN 0xD6 binary format). Validates protocol implementation.

**Loopback performance**: UDP loopback is fast (~1ms), overhead negligible compared to 93ms physics tick rate.

**External simulator option**: External GPS simulator can also connect via UDP (same port). Backend doesn't distinguish built-in vs external simulator.

## Consequences

### Positive
- ✅ **Tests full pipeline**: UDP reception, binary unpacking, GPS processing all tested
- ✅ **Realistic testing**: Mimics AgIO behavior exactly
- ✅ **Bug detection**: Integration tests catch network and protocol issues
- ✅ **Protocol validation**: Proves PGN 0xD6 encoding/decoding works
- ✅ **External simulator support**: Any UDP sender can provide GPS (not just built-in simulator)

### Negative
- ❌ **Slight overhead**: UDP loopback adds ~1ms latency (negligible)
- ❌ **More code**: Simulator needs protocol serializer (AgIoProtocolSerializer)
- ❌ **Complex simulator**: Simulator knows about binary protocol (infrastructure concern)

### Neutral
- ⚪ **Localhost only**: Simulator sends to 127.0.0.1 (loopback, no external network)
- ⚪ **Port 15556**: Same port as AgIO (backend can't distinguish source)

## Implementation

**Delivered in**: Workflow 002 (GPS/GNSS Migration)

**Key components**:
- **SimulatorService**: Generates GPS data, calls serializer, sends UDP
- **AgIoProtocolSerializer**: Encodes GPS data to PGN 0xD6 binary format
- **SimulatorHostedService**: Timer (93ms), calls SimulatorService.Tick(), sends UDP packets
- **UdpClient**: .NET UdpClient sends to localhost:15556

**Data flow**:
```
Simulator physics → GPS data (value objects)
  → AgIoProtocolSerializer → Binary packet (128 bytes PGN 0xD6)
  → UDP send to localhost:15556
  → UdpPacketReceiver receives
  → ApplicationOrchestrator processes
  → GnssService unpacks binary
  → State broadcasted
```

**Loopback communication**:
- Simulator and backend in same process (AgOpenGPS.Api)
- UDP sent to 127.0.0.1 (loopback interface, no external network)
- Instant delivery (loopback doesn't go through physical network)
- Never drops packets (loopback is reliable)

**Commit reference**: See Workflow 002 task 6 (Implement GnssService) and simulator UDP implementation

**Date**: Workflow 002 completion (2025)

## Related Decisions

- **[001-event-driven-architecture.md](001-event-driven-architecture.md)**: UDP packets trigger processing (event-driven)
- **[005-simulator-domain-separation.md](005-simulator-domain-separation.md)**: AgIoProtocolSerializer is infrastructure (not domain)
- **[011-testing-strategy.md](../11-testing-strategy.md)**: Integration tests use simulator (validates full pipeline)

## System Documentation

- **[../05-network-communication.md](../05-network-communication.md)**: UDP communication details
- **[../06-simulator-capabilities.md](../06-simulator-capabilities.md)**: Simulator overview (mentions UDP)
- **[../10-simulator-domain.md](../10-simulator-domain.md)**: AgIoProtocolSerializer bounded context

## References

- **Workflow 002 Plan**: [../../workflow/002-gps-gnss-migration/plan.md](../../../workflow/002-gps-gnss-migration/plan.md)
- **AgOpenGPS protocol**: PGN 0xD6 binary format (128 bytes)
- **Loopback interface**: 127.0.0.1, OS-internal routing (no physical network)
