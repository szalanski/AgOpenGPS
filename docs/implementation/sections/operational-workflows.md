# Operational Workflows

## Startup Sequence

1. **Backend boots** - Hosted services start automatically. The simulator remains idle until commanded.
2. **Clients connect** - FormGPS (or other consumers) opens a SignalR connection to `/statehub`.
3. **Field origin configured** - Either a persisted setting or user action selects the local-plane origin. Until this occurs, the backend withholds GNSS broadcasts.
4. **GNSS feed begins** - Hardware or the simulator pushes PGN 0xD6 packets into the UDP listener, activating the steady-state loop.

This sequence ensures that by the time state starts flowing, both coordinate frames and communication channels are ready.

## GNSS to Client Broadcast Flow

1. **Packet reception** - The UDP listener validates structure and hands an `UdpPacket` to the orchestrator.
2. **Domain update** - `GnssService` converts the payload into the domain snapshot, updating health and motion metrics.
3. **State assembly** - `ApplicationState` is constructed with the latest GNSS data and a monotonic UTC timestamp.
4. **Broadcast** - `SignalRStatePublisher` emits the state to all active connections using the `ReceiveState` event.
5. **Client handling** - FormGPS merges the state into its frontend model, updates UI widgets, and triggers guidance logic.

The cadence is entirely packet-driven; there is no auxiliary timer. If packets arrive at 10 Hz, clients receive 10 updates per second.

## Frontend GNSS Integration Flow

FormGPS consumes backend GPS data through an adapter pattern that preserves compatibility with existing UI code while implementing the Strangler Fig migration strategy.

1. **State reception** - FormGPS.OnStateReceived (SourceCode/GPS/Forms/FormGPS.cs:587-613) receives ApplicationState from SignalR and caches it in the _cachedState field (line 94).
2. **Adapter invocation** - OnStateReceived calls CNMEA.UpdateFromBackendState (SourceCode/GPS/Classes/CNMEA.cs:37-60) to translate backend GPS data into legacy field references.
3. **Field mapping** - The adapter populates legacy CNMEA fields:
   - Backend LocalPosition.Easting/Northing maps to fix.easting/northing
   - Backend Speed.KilometersPerHour maps to speed and vtgSpeed
   - Backend Altitude.Meters maps to altitude
   - Backend Heading (single/dual) maps to headingTrue and headingTrueDual
   - Backend Quality metrics map to fixQuality, satellitesTracked, hdop, and age
4. **UI rendering** - Existing UI code continues to reference legacy fields (pn.fix, pn.speed, etc.) without modification. All labels, displays, and guidance calculations work transparently through the adapter.
5. **Legacy bypass** - When the backend connection is active, UDPComm.Designer.cs (line 60-66) guards against legacy UDP GPS processing, ensuring the backend remains the single source of truth.

This pattern allows zero breaking changes to existing code while gradually migrating GPS processing to the backend. The adapter serves as a temporary bridge that can be removed once all FormGPS GPS references are refactored to consume ApplicationState directly.

## Simulator Control Loop

1. **User intent** - A UI control sends a simulator command (start, stop, speed change, and so on) through SignalR as `UpdateSimulatorCommand`.
2. **Command dispatch** - The hub routes the command to MediatR. The handler invokes `SimulatorService.ProcessEvent`.
3. **State mutation** - Simulator services adjust their internal state, enabling or pausing tick generation.
4. **Packet synthesis** - `SimulatorHostedService` ticks every 93 ms, producing PGN 0xD6 packets identical to hardware output and sending them back over UDP.
5. **Feedback loop** - Generated packets re-enter the GNSS workflow, giving clients immediate visual confirmation.

Because the simulator feeds the same ingress pipeline, tests and demos exercise realistic end-to-end behaviour.

## Failure and Recovery Scenarios

- **Packet loss** - When UDP packets drop, the backend continues broadcasting the last known state but health metrics (frequency, watchdog counter) reveal the issue. Clients may choose to gray-out guidance or fall back to dead-reckoning.
- **Backend restart** - Upon restart, clients reconnect via SignalR automatically. The orchestrator requires at least one valid GNSS packet post-restart before resuming broadcasts, ensuring stale data is not reused.
- **Simulator misuse** - If invalid commands are issued (for example missing payload), the handler logs the failure and no state changes occur. Legitimate commands remain unaffected because each event is validated before execution.

## Planned Workflow Extensions

- **IMU integration** - Additional workflows will route PGN 0xD3 packets into orientation fusion, enriching the state broadcast with roll and pitch data.
- **AutoSteer commands** - The command path will expand beyond the simulator, sending intent to real hardware modules once safety and validation layers are in place.
- **Field lifecycle** - Upcoming work introduces workflows for switching fields mid-session, including origin updates and persisted state handoff.
