# ADR 001: Event-Driven Architecture

## Status
**Accepted**

## Context

The backend needs a main processing loop that:
- Receives GPS data from external sources (AgIO via UDP)
- Processes GPS data (unpacking, transformations, state computation)
- Broadcasts state updates to connected clients

Traditional approach uses **fixed-rate timer**:
- Timer fires every 250ms (4 Hz)
- Read GPS data if available
- Process and broadcast
- Repeat on next timer tick

**Problems with timer-based approach**:
- Processing happens even when no new GPS data (wasted cycles)
- New GPS data waits for next timer tick (latency)
- Fixed rate doesn't match GPS reality (GPS rate varies 5-10 Hz)
- Timer in UI thread couples processing to rendering rate

## Decision

**ApplicationOrchestrator is GPS-driven** (not timer-based). GPS packet arrival triggers all processing.

## Options Considered

### Option 1: Fixed-Rate Timer (Legacy Approach)
**Description**: Timer fires at 4 Hz, check for GPS data each tick

**Pros**:
- Predictable timing (always 250ms intervals)
- Simple to understand (familiar pattern)
- Bounded CPU usage (fixed rate)

**Cons**:
- Latency (up to 250ms if GPS arrives just after timer)
- Wasted cycles (timer fires even if no GPS data)
- Rate mismatch (GPS is 5-10 Hz, timer is 4 Hz)
- Couples processing rate to arbitrary timer

### Option 2: Polling Loop
**Description**: Busy-wait loop checking for GPS data continuously

**Pros**:
- Minimal latency (checks constantly)
- No timer overhead

**Cons**:
- CPU waste (100% CPU even when idle)
- Power inefficient
- Not scalable (one thread per data source)

### Option 3: Event-Driven (GPS Packet Arrival) ✅ **CHOSEN**
**Description**: Async streaming, GPS packet arrival triggers processing

**Pros**:
- Zero latency (process immediately when GPS arrives)
- Efficient (CPU sleeps when no data)
- Matches GPS reality (variable rate)
- Scalable (event-driven, non-blocking)

**Cons**:
- Variable timing (not predictable intervals)
- More complex (async/await, IAsyncEnumerable)
- Testing requires actual packets (not just timer mocks)

## Rationale

**GPS reality**: Real GPS data arrives at variable rate (5-10 Hz typical, depends on receiver). Processing should match this reality, not artificial fixed rate.

**Precision agriculture timing**: Operators need current position NOW (for steering corrections), not stale position delivered at next timer tick.

**Backend independence**: Backend shouldn't have arbitrary timer (UI refresh rate concern). Backend should respond to data sources, not internal timers.

**Efficiency**: Backend sleeps when no data (typical agriculture scenario: tractor stopped, no GPS changes). Timer approach wastes CPU cycles.

**Scalability**: Event-driven architecture scales to multiple data sources (GPS, IMU, steering feedback) without complex timer coordination.

## Consequences

### Positive
- ✅ **Minimal latency**: GPS packet processed <5ms after arrival (vs up to 250ms with timer)
- ✅ **Efficient**: No CPU usage when idle (tractor stopped, no GPS packets)
- ✅ **Realistic**: Processing rate matches GPS reality (variable 5-10 Hz)
- ✅ **Headless-ready**: Backend doesn't depend on UI refresh rate
- ✅ **Scalable**: Easy to add more data sources (IMU, steering) with same pattern

### Negative
- ❌ **Variable timing**: State updates at GPS rate (not predictable 4 Hz)
- ❌ **Testing complexity**: Tests need to send actual packets (can't just mock timer)
- ❌ **Async complexity**: More complex code (async/await, IAsyncEnumerable)

### Neutral
- ⚪ **No fixed rate**: Frontend receives variable-rate updates (typically 5-10 Hz)
- ⚪ **UDP-dependent**: System idle if no UDP packets (intentional design)

## Implementation

**Delivered in**: Workflow 002 (GPS/GNSS Migration)

**Key changes**:
- ApplicationOrchestrator uses `IAsyncEnumerable<UdpPacket>` (not timer)
- Main loop: `await foreach (var packet in _udpReceiver.GetPacketsAsync())`
- UDP receiver yields packets as they arrive (async streaming)
- Processing triggered by packet arrival (not timer tick)

**Commit reference**: See Workflow 002 task 5-6 (UDP Listener + GPS Service implementation)

**Date**: Workflow 002 completion (2025)

## Related Decisions

- **[002-backend-driven-timing.md](002-backend-driven-timing.md)**: Why backend owns timing (not frontend)
- **[007-udp-communication-pattern.md](007-udp-communication-pattern.md)**: Why simulator also uses UDP (tests event-driven path)

## System Documentation

- **[../02-main-processing-loop.md](../02-main-processing-loop.md)**: How event-driven loop works
- **[../04-gnss-data-pipeline.md](../04-gnss-data-pipeline.md)**: GPS processing triggered by packets
- **[../05-network-communication.md](../05-network-communication.md)**: UDP packet reception details

## References

- **Workflow 002 Plan**: [../../workflow/002-gps-gnss-migration/plan.md](../../../workflow/002-gps-gnss-migration/plan.md)
- **Reactive programming**: Event-driven architecture matches reactive principles
- **Agriculture context**: GPS arrival rate varies (5 Hz standard, 10 Hz RTK, 1 Hz low-cost)
