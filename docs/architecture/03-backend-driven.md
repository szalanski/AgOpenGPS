# Backend-Driven, Event-Driven Architecture

## Overview

The API backend is the single source of truth for GNSS processing and application state. Instead of relying on WinForms timers, the backend reacts to UDP packets as they arrive, derives domain state, and broadcasts updates to every connected client. Frontends such as FormGPS render the streamed state and issue high-level commands.

## Runtime Flow

1. **UDP intake** - `UdpPacketReceiver` listens on the configured port, validates AgIO framing, and yields packets as an async stream.
2. **Orchestration** - `ApplicationOrchestrator` consumes the stream, initialises the local plane once, and dispatches packets to the appropriate domain services.
3. **GNSS processing** - `GnssService` unpacks PGN 0xD6 payloads, performs coordinate transforms, updates motion and quality metrics, and caches the latest `GnssState`.
4. **State publication** - `ApplicationState` is assembled with the GNSS snapshot and timestamp, then `SignalRStatePublisher` sends it to all clients via `/statehub`.
5. **Client reaction** - FormGPS and other consumers update UI or automation logic as soon as `ReceiveState` fires. No polling or local GNSS decoding is required.

```
AgIO or Simulator --> UDP --> UdpPacketReceiver
UdpPacketReceiver --> ApplicationOrchestrator
ApplicationOrchestrator --> GnssService
GnssService --> ApplicationOrchestrator
ApplicationOrchestrator --> SignalRStatePublisher
SignalRStatePublisher --> Connected Clients
```

## Key Responsibilities

- **UdpPacketReceiver** (SourceCode/AgOpenGPS.Api/Services/UdpPacketReceiver.cs)  
  Owns the socket and enforces packet validity (header, length, checksum). Exposes packets via `IAsyncEnumerable`.

- **ApplicationOrchestrator** (SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs)  
  Runs as a hosted service, initialises the coordinate transformer, consumes packets, and broadcasts only when the GNSS service reports a valid state.

- **GnssService** (SourceCode/AgOpenGPS.Api/Services/GnssService.cs)  
  Transforms binary telemetry into domain objects, calculates frequency and health metrics, and keeps snapshots immutable once published.

- **SignalRStatePublisher** (SourceCode/AgOpenGPS.Api/Services/SignalRStatePublisher.cs)  
  Provides the transport abstraction (`IStatePublisher`), pushing `ApplicationState` updates to all SignalR clients and logging failures without interrupting the loop.

- **SimulatorHostedService** (SourceCode/AgOpenGPS.Api/Services/SimulatorHostedService.cs)  
  Optional component that generates realistic PGN 0xD6 packets every 93 ms and injects them into the same UDP path, proving the full pipeline without hardware.

## Operational Characteristics

- **Latency** - Packet-to-broadcast latency is dominated by UDP delivery plus in-memory processing (typically a few milliseconds on localhost).
- **Cadence** - Update frequency tracks the upstream GNSS source. The simulator and common receivers produce roughly 10 Hz, but the pipeline handles higher rates.
- **Resilience** - Lost packets are detected via `GpsHealth` metrics (smoothed frequency, watchdog counter). The backend never fabricates interpolated samples.
- **Configurability** - `UdpOptions` controls port and buffer size; `appsettings.json` defaults to 15556 to avoid clashing with legacy FormGPS during migration.
- **Extensibility** - Additional PGNs (IMU, AutoSteer) can plug into the orchestrator by adding new domain services and publishing richer state payloads.

## Client Expectations

- FormGPS keeps `tmrWatchdog` for UI housekeeping only; all GNSS-derived values must come from `ApplicationState.Gnss`.
- New clients should reuse the shared models from `AgOpenGPS.Api.Client` to remain contract-safe.
- Commands (currently simulator control) flow back through `StateHub` and MediatR, keeping transport logic thin and testable.

## Related Documentation

- Implementation details: `docs/implementation/sections/system-architecture.md`
- GNSS domain model: `docs/implementation/sections/gnss-capabilities.md`
- Communication contracts: `docs/implementation/sections/communication-protocols.md`
