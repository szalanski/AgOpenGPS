# Operational Workflows

## Startup Sequence

1. **Backend boots** - Hosted services start automatically. `SimulatorHostedService` and `ApplicationOrchestrator` are registered by default.
2. **Local plane primed** - `ApplicationOrchestrator` currently initialises the local plane with a placeholder origin (45°N, 93°W) until a configuration workflow is introduced (`SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs:36`).
3. **Clients connect** - FormGPS uses `IBackendClient.ConnectAsync` to open a SignalR connection to `/statehub` (`SourceCode/GPS/Forms/FormGPS.cs:570`).
4. **GNSS feed begins** - Hardware or the simulator pushes PGN 0xD6 packets into the UDP listener, activating the event-driven loop.

This sequence ensures that by the time state starts flowing, both coordinate frames and communication channels are ready.

## GNSS to Client Broadcast Flow

1. **Packet reception** - `UdpPacketReceiver.GetPacketsAsync` validates structure and yields a typed packet (`SourceCode/AgOpenGPS.Api/Services/UdpPacketReceiver.cs:39`).
2. **Domain update** - `GnssService.ProcessGpsPacket` converts the payload into a snapshot, including derived motion and health metrics (`SourceCode/AgOpenGPS.Api/Services/GnssService.cs:33`).
3. **State assembly** - `ApplicationOrchestrator.ProcessPacketAsync` wraps the GNSS snapshot in `ApplicationState` and stamps the broadcast time (`SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs:63`).
4. **Broadcast** - `SignalRStatePublisher.BroadcastStateAsync` pushes the state to all active connections via the `ReceiveState` hub method (`SourceCode/AgOpenGPS.Api/Services/SignalRStatePublisher.cs:24`).
5. **Client handling** - FormGPS caches the state in `_cachedState` and updates legacy CNMEA fields through the adapter, allowing existing UI code to continue reading from `pn` (`SourceCode/GPS/Forms/FormGPS.cs:587` and `SourceCode/GPS/Classes/CNMEA.cs:37`).

The cadence is entirely packet-driven; there is no auxiliary timer. If packets arrive at 10 Hz, clients receive 10 updates per second.

## Frontend GNSS Integration Flow

FormGPS consumes backend GPS data through an adapter pattern that preserves compatibility with existing UI code while implementing the Strangler Fig migration strategy.

1. **State reception** - `FormGPS.OnStateReceived` receives `ApplicationState` from SignalR and caches it in `_cachedState` (`SourceCode/GPS/Forms/FormGPS.cs:587`).
2. **Adapter invocation** - The handler calls `CNMEA.UpdateFromBackendState` to translate backend GPS data into legacy field references (`SourceCode/GPS/Classes/CNMEA.cs:37`).
3. **Field mapping** - The adapter populates legacy CNMEA fields:
   - Backend LocalPosition.Easting/Northing maps to fix.easting/northing
   - Backend Speed.KilometersPerHour maps to speed and vtgSpeed
   - Backend Altitude.Meters maps to altitude
   - Backend Heading (single/dual) maps to headingTrue and headingTrueDual
   - Backend Quality metrics map to fixQuality, satellitesTracked, hdop, and age
4. **UI rendering** - Existing UI code continues to reference `pn` fields. No labels are updated directly inside `OnStateReceived`; the adapter keeps legacy drawing routines fed with backend data.
5. **Legacy bypass** - `ReceiveFromAgIO` short-circuits legacy UDP processing when `_backendClient.IsConnected` returns `true`, but still supports fallback when the backend is offline (`SourceCode/GPS/Forms/UDPComm.Designer.cs:63`).

This pattern allows zero breaking changes to existing code while gradually migrating GPS processing to the backend. The adapter serves as a temporary bridge that can be removed once all FormGPS GPS references are refactored to consume ApplicationState directly.

## Simulator Control Loop

1. **Command availability** - `SignalRBackendClient.SendCommandAsync` supports sending `UpdateSimulatorCommand` instances to the hub (`SourceCode/AgOpenGPS.Api.Client/SignalR/SignalRBackendClient.cs:51`). FormGPS does not yet invoke this API, but future UI work can wire button handlers to these commands.
2. **Hub dispatch** - `StateHub.UpdateSimulator` forwards commands to MediatR (`SourceCode/AgOpenGPS.Api/Hubs/StateHub.cs:54`).
3. **Service processing** - `UpdateSimulatorCommandHandler` delegates to `SimulatorService.ProcessEvent` to mutate simulator state (`SourceCode/AgOpenGPS.Api/Commands/Handlers/UpdateSimulatorCommandHandler.cs:27`).
4. **Packet synthesis** - `SimulatorHostedService` ticks every 93 ms, generates PGN 0xD6 packets, and sends them over UDP to reuse the production pipeline (`SourceCode/AgOpenGPS.Api/Services/SimulatorHostedService.cs:26`).
5. **Feedback loop** - Generated packets re-enter the GNSS workflow, so any client (once wired) immediately observes the simulator response.

Because the simulator feeds the same ingress pipeline, tests and demos can exercise realistic end-to-end behaviour even before FormGPS exposes the new controls.

## Failure and Recovery Scenarios

- **Packet loss** - When UDP packets drop, the backend continues broadcasting the last known state but health metrics (frequency, watchdog counter) reveal the issue. Clients may choose to gray-out guidance or fall back to dead-reckoning.
- **Backend restart** - Upon restart, clients reconnect via SignalR automatically. The orchestrator requires at least one valid GNSS packet post-restart before resuming broadcasts, ensuring stale data is not reused.
- **Simulator misuse** - If invalid commands are issued (for example missing payload), the handler logs the failure and no state changes occur. Legitimate commands remain unaffected because each event is validated before execution.

## Planned Workflow Extensions

- **IMU integration** - Additional workflows will route PGN 0xD3 packets into orientation fusion, enriching the state broadcast with roll and pitch data.
- **AutoSteer commands** - The command path will expand beyond the simulator, sending intent to real hardware modules once safety and validation layers are in place.
- **Field lifecycle** - Upcoming work introduces workflows for switching fields mid-session, including origin updates and persisted state handoff.
