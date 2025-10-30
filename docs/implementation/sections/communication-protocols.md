# Communication Protocols

## Landscape Overview

The migration introduced a layered communication model that keeps legacy hardware compatibility while centralising business logic in the backend API. Two channels matter today:

- **UDP (AgIO compatible)** - Carries low-level packet data from receivers or the simulator into the backend.
- **SignalR WebSocket** - Broadcasts derived application state and accepts high-level control commands from FormGPS or other clients.

Understanding both channels is critical for assessing interoperability, latency, and failure handling.

## AgIO UDP Format (PGN 0xD6)

| Field                | Bytes | Description                                                                              |
|----------------------|-------|------------------------------------------------------------------------------------------|
| Header               | 0-1   | Fixed 0x80 0x81 guard that identifies AgIO packets                                       |
| Source address       | 2     | Legacy source identifier (0x7F for loopback) retained for compatibility                  |
| PGN                  | 3     | 0xD6 marks GNSS data; other PGNs (IMU, AutoSteer) are reserved for future expansion      |
| Payload length       | 4     | Number of payload bytes (currently 51)                                                   |
| Payload              | 5-55  | Serialised GNSS measurements (positions, headings, motion, quality metrics)              |
| Checksum             | 56    | Sum of bytes 2-55, providing lightweight corruption detection                             |

**Coordinate frames**

- Longitude and latitude are IEEE 754 doubles on the WGS84 datum.
- Local plane easting/northing are derived in the backend once a field origin is known.
- Heading fields distinguish dual-antenna (if present) from true course.

**Reliability traits**

- Packets rely on best-effort UDP; the backend does not request retransmission. Health metrics such as `GpsHealth.Hz` and watchdog counters compensate by detecting dropouts.
- The checksum is sufficient to reject malformed datagrams without cryptographic guarantees.

## Backend UDP Listener Contracts

- **Port allocation** - Configurable through `UdpOptions.ListenPort`. Default 15555 mirrors legacy FormGPS, while the default appsettings override (15556) isolates the backend during transition.
- **Validation rules** - Header, length, and checksum must pass. Invalid packets are logged with context and discarded, maintaining downstream invariants.
- **Packet types** - Although only GNSS packets are consumed today, the mapping table preserves additional AgIO PGNs so IMU, AutoSteer, and sensor data can activate later without protocol forks.

## SignalR State Channel

- **Hub endpoint** - `/statehub` exposes both outbound state and inbound simulator commands.
- **Message contract** - `ApplicationState` is serialised with camelCase properties; GNSS payloads mirror the shared client value objects (`GnssState`, `GpsHealth`, `GpsQuality`).
- **Update pace** - The orchestrator publishes state whenever new GNSS data arrives. On clean 10 Hz feeds, that produces roughly 10 messages per second. Consumers should treat the timestamp as the source of truth for ordering.
- **Command path** - `UpdateSimulatorCommand` wraps simulator events (start, stop, speed changes). The hub forwards commands to MediatR (`SourceCode/AgOpenGPS.Api/Hubs/StateHub.cs:54`), which in turn delegates to `SimulatorService` (`SourceCode/AgOpenGPS.Api/Commands/Handlers/UpdateSimulatorCommandHandler.cs:27`).

## Frontend Client Library (AgOpenGPS.Api.Client)

The shared client library (.NET Standard 2.0) provides FormGPS and other consumers with strongly-typed access to backend services. It abstracts transport details behind interfaces, enabling future protocol swaps without affecting application code.

**Core Abstractions**

- **IBackendClient** (SourceCode/AgOpenGPS.Api.Client/Abstractions/IBackendClient.cs:14) - Defines the bidirectional communication contract (`ConnectAsync`, `SubscribeToState`, `SendCommandAsync`, `IsConnected`, and disposal members).

- **SignalRBackendClient** (SourceCode/AgOpenGPS.Api.Client/SignalR/SignalRBackendClient.cs:18) - Registers the `ReceiveState` handler, exposes subscription APIs, and implements `SendCommandAsync`. At present the switch statement only supports `UpdateSimulatorCommand` (`SourceCode/AgOpenGPS.Api.Client/SignalR/SignalRBackendClient.cs:66`), ensuring unsupported commands throw clear errors.

- **BackendClientFactory** (SourceCode/AgOpenGPS.Api.Client/Factories/BackendClientFactory.cs:17) - Creates a configured SignalR client using `ConnectionOptions`. Automatic reconnection is enabled by default.

**Usage Pattern**

FormGPS establishes the connection during startup (`SourceCode/GPS/Forms/FormGPS.cs:561-572`):
- Builds `ConnectionOptions` (URL defaults to `http://localhost:5000` in `ConnectionOptions.Url`).
- Creates an `IBackendClient` via `BackendClientFactory.CreateSignalRClient`.
- Subscribes to state updates with `OnStateReceived`.
- Calls `ConnectAsync` and logs the connection result.

**State Payload**

- `Gnss` carries the decoded GPS snapshot.
- `LocalPlane` exposes origin and conversion factors so the frontend can synchronise its coordinate system (`SourceCode/GPS/Forms/FormGPS.cs:605`).
- `Control` currently reports the simulator steering angle, which the frontend applies to the wheel model (`SourceCode/GPS/Forms/FormGPS.cs:659`).

- **Current frontend behaviour** - `OnStateReceived` caches the latest state, synchronises the plane, updates legacy fields, and triggers geometry recomputation. Simulator controls now dispatch `UpdateSimulatorCommand` instances through `_backendClient.SendCommandAsync` (`SourceCode/GPS/Forms/Controls.Designer.cs:2100`).

**Graceful Disconnection**

When the backend disconnects, the client stops receiving state updates but remains resident. FormGPS checks `_backendClient.IsConnected` before deciding whether to bypass legacy UDP processing (`SourceCode/GPS/Forms/UDPComm.Designer.cs:63`).

## Client Expectations

- **FormGPS** should treat SignalR state as canonical and fall back to legacy paths only if the backend disconnects.
- **Hardware integrations** can reuse the UDP format: any sender that respects the PGN structure can feed the backend without additional adapters.
- **New consumers** should reuse the shared client models to avoid contract drift.

## Future Protocol Enhancements

- **IMU integration** - PGN 0xD3 packets are validated but not yet processed. Extending the orchestrator to branch on IMU data will require documenting payload interpretation here.
- **Authentication** - Both channels currently assume trusted localhost usage. Once cross-machine deployment begins, revisit TLS for SignalR and optional packet signing for UDP.
- **Backpressure** - For higher-rate GNSS feeds (greater than 20 Hz), consider batching or delta compression on the SignalR side to avoid overwhelming slower UI clients.
