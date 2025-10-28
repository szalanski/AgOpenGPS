# Simulator Architecture

## Purpose

The simulator provides a deterministic way to exercise the entire backend pipeline without field hardware. It aims to:

- Mimic the kinematics of a vehicle operating in open field conditions.
- Produce AgIO-compliant packets so production code paths remain identical between simulation and hardware.
- Give operators immediate feedback when experimenting with guidance behaviours or latency-sensitive features.

## Control Surface

- **Activation** - The simulator is disabled on boot and only starts emitting packets after a `Start` event enables it (`SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:44`).
- **Command ingestion** - `SimulatorService.ProcessEvent` interprets `UpdateSimulatorCommand` events from MediatR and mutates internal state accordingly (`SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:173`). FormGPS does not yet call `IBackendClient.SendCommandAsync`, but the SignalR client supports sending `UpdateSimulatorCommand` when UI handlers are wired (`SourceCode/AgOpenGPS.Api.Client/SignalR/SignalRBackendClient.cs:51`).
- **Safety rails** - Speeds are clamped between -21 and 322 km/h and steering changes are eased to mirror legacy behaviour (`SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs:57`). This prevents unrealistic motion that would skew downstream testing.

## Motion Model

- **Kinematic integration** - Every 93 ms tick computes distance travelled from speed, applies steering curvature, and updates heading using a great-circle calculation. This keeps long-distance behaviour realistic.
- **Smoothing functions** - Speed and steering transitions reuse the FormGPS easing rates (`TransitionSpeed`, `SmoothSteeringAngle`), ensuring UI behaviour (such as guidance line convergence) matches production expectations.
- **Telemetry generation** - Altitude, satellite counts, fix quality, HDOP, and correction age are deterministic yet plausible, giving the rest of the system meaningful numbers during simulation.

## Packet Output

- **Format fidelity** - Packets are byte-for-byte compatible with AgIO PGN 0xD6 via `AgIoProtocolSerializer.EncodeGpsDataPacket` (`SourceCode/AgOpenGPS.Api/Services/AgIoProtocolSerializer.cs:17`). Fields the simulator cannot generate (dual antenna heading, IMU) deliberately use sentinel values so `GnssService` treats them the same as real receivers lacking those capabilities.
- **Transport path** - `SimulatorHostedService.ExecuteAsync` sends packets over UDP to the same port that hardware uses, allowing the orchestrator, GNSS service, and publisher to behave identically regardless of source (`SourceCode/AgOpenGPS.Api/Services/SimulatorHostedService.cs:26`).
- **Cadence** - The 93 ms interval mirrors the legacy timer; combined with the GNSS frequency filter in `GnssService`, observed cadence converges on roughly 10.75 Hz (`SourceCode/AgOpenGPS.Api/Services/GnssService.cs:36`).

## Usage Scenarios

- **Integration testing** - Automated tests and developers can validate SignalR clients, command handling, or GNSS processing without specialised equipment.
- **Demo and training mode** - Operators can demonstrate the system indoors while still showing realistic motion and signal quality.
- **Regression protection** - Because the simulator feeds the production pipeline, changes that break packet parsing, coordinate conversion, or publishing are caught even when hardware is unavailable.

## Known Limitations

- The motion model assumes flat terrain and ignores slip or implement drag.
- RTK failure modes (for example correction age spikes or satellite dropouts) are not yet simulated; targeted scenarios will be needed to exercise those behaviours.
- Only a single simulated vehicle exists; multi-vehicle coordination will require extending the architecture to differentiate packet sources.
