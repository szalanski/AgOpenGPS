# System Architecture

## Context and Actors

AgOpenGPS positions the API backend as the coordination hub between three primary actors:

1. **Field hardware and AgIO** - capture GNSS and equipment telemetry, emitting AgIO-compatible UDP packets.
2. **Backend API** - ingests raw packets, applies domain logic, and exposes a consolidated state surface.
3. **FormGPS / UI clients** - consume derived state and issue intent-level commands (for example simulator controls or future steering directives).

This split keeps hardware integration stable while letting the backend evolve independently of the desktop application.

## Bounded Responsibilities

- **UDP intake boundary** - `UdpPacketReceiver` owns socket lifecycle and protocol validation, ensuring downstream services only see well-formed packets.
- **Domain processing core** - `ApplicationOrchestrator` and the services behind it (`GnssService`, forthcoming IMU handlers) transform transport-level packets into domain objects and enforce invariants such as "no broadcast without a valid local plane".
- **State surface** - `SignalRStatePublisher` hides SignalR mechanics from the rest of the system, paving the way for alternate publishers (REST snapshots, message queues) without touching domain logic.
- **Command intake** - The SignalR hub plus MediatR translate user intent (currently simulator-only) into domain events. Intent is modelled explicitly so future equipment-control commands can reuse the same flow.

## Data Flow Overview

1. A packet arrives over UDP on the configured port. Hardware, the simulator, and integration tests all share this contract.
2. Edge validation filters out malformed frames using header, length, and checksum checks.
3. The orchestrator routes recognised PGNs to the appropriate domain subsystem. Today that path focuses on GNSS, with IMU and AutoSteer to follow.
4. Domain services recompute derived values (frequency, local coordinates, health) and refresh the in-memory snapshot.
5. State is broadcast via SignalR to every connected client. The cadence follows the incoming packet frequency.
6. Commands flow back through the hub, into MediatR, and finally into simulator services that mutate their own internal state under lock.

## Operational Qualities

- **Latency** - UDP ingestion plus in-memory processing keeps end-to-end latency under a few milliseconds on localhost. SignalR introduces typical WebSocket jitter, but broadcasts happen as soon as each packet is processed.
- **Isolation** - Default configuration listens on port 15556 so FormGPS can remain on 15555 during migration. After cutover, both sides can converge on a shared port if desired.
- **Resilience** - Because UDP is best-effort, resilience comes from health metrics rather than retransmission. The orchestrator and publisher tolerate bursty or sparse traffic; only malformed packets are discarded.
- **Testability** - Hosted services start automatically in production, while integration tests can override dependencies or configuration thanks to the options pattern and MediatR pipeline.

## Deployment Assumptions

- Current deployments assume localhost communication between FormGPS and the backend, so firewall and TLS requirements are minimal.
- Scaling out to multiple backend instances will require a shared state layer or message bus so broadcasts remain consistent. The architecture intentionally keeps state on a single host for now to avoid premature distribution complexity.

## Configuration Considerations

- `UdpOptions` centralises UDP port and buffer settings, making it straightforward to align the backend with different AgIO installations.
- Logging defaults balance operational visibility with noise: Information level for transport boundaries, Debug inside high-frequency loops.
