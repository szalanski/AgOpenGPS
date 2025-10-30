# Frontend Integration Guide

This document describes how FormGPS integrates with the backend API to consume GPS data and control the simulator, implementing the Strangler Fig migration pattern through an adapter layer.

## Overview

FormGPS connects to the backend via SignalR, receives real-time GPS state updates, and translates them into legacy field references through an adapter. This approach enables backend-driven GPS processing without breaking existing UI code.

**Key Architectural Patterns:**

- **Strangler Fig** - Backend gradually replaces legacy FormGPS behavior while maintaining compatibility
- **Adapter Pattern** - CNMEA class bridges backend ApplicationState and legacy fields
- **Transport Abstraction** - IBackendClient interface enables future protocol swaps

## Connection Establishment

FormGPS establishes a backend connection during startup through the following sequence (SourceCode/GPS/Forms/FormGPS.cs:561-570):

1. Creates ConnectionOptions with backend hub URL (http://localhost:5000/statehub)
2. Uses BackendClientFactory to create SignalRBackendClient instance
3. Subscribes to state updates with OnStateReceived callback
4. Calls ConnectAsync to establish SignalR connection
5. Backend begins broadcasting ApplicationState updates at GPS packet cadence (typically 10 Hz)

The connection handles automatic reconnection if the backend restarts, ensuring resilient operation during development and deployment.

## State Reception and Caching

When the backend broadcasts ApplicationState, FormGPS caches it for UI thread access and immediately synchronises coordinate and steering metadata (SourceCode/GPS/Forms/FormGPS.cs:587-665).

**OnStateReceived Flow:**

1. **Thread marshaling** - If called from SignalR thread, marshal to UI thread via BeginInvoke
2. **State caching** - Store ApplicationState in _cachedState field (line 94)
3. **Null check** - Validate state.Gnss is not null before processing
4. **Local plane synchronisation** - On the first valid packet, read `state.LocalPlane.Origin` and call `pn.DefineLocalPlane` so the frontend uses the backend-origin plane (SourceCode/GPS/Forms/FormGPS.cs:605)
5. **Adapter invocation** - Call pn.UpdateFromBackendState(state) to populate legacy fields (SourceCode/GPS/Classes/CNMEA.cs:37)
6. **Steering feedback** - If `state.Control` is present, update `mc.actualSteerAngleDegrees` for wheel rendering before recalculating position (SourceCode/GPS/Forms/FormGPS.cs:659)
7. **Position update** - Invoke `UpdateFixPosition()` which now reads directly from `_cachedState` to rebuild vehicle state (SourceCode/GPS/Forms/Position.designer.cs:128)
8. **Logging** - Log GPS data receipt and diagnostic details (SourceCode/GPS/Forms/FormGPS.cs:667)

SignalR callbacks arrive on a background thread. OnStateReceived uses InvokeRequired and BeginInvoke to ensure all UI updates and field modifications happen on the UI thread, preventing race conditions.

## Adapter Pattern Implementation

The CNMEA class acts as an adapter between backend GPS data and legacy FormGPS field references (SourceCode/GPS/Classes/CNMEA.cs:37-60).

**Field Mapping:**

| Backend Property | Legacy Field | Unit | Description |
|------------------|--------------|------|-------------|
| state.Gnss.LocalPosition.Easting | fix.easting | meters | Local plane X coordinate |
| state.Gnss.LocalPosition.Northing | fix.northing | meters | Local plane Y coordinate |
| state.Gnss.Speed.KilometersPerHour | speed, vtgSpeed | km/h | Ground speed |
| state.Gnss.Altitude.Meters | altitude | meters | Elevation above WGS84 ellipsoid |
| state.Gnss.HeadingSingle.Degrees | headingTrue | degrees | True course (0-360) |
| state.Gnss.HeadingDual.Degrees | headingTrueDual | degrees | Dual-antenna heading |
| state.Gnss.Quality.FixQuality | fixQuality | enum int | NMEA fix quality (0-8) |
| state.Gnss.Quality.SatellitesTracked | satellitesTracked | count | Number of satellites |
| state.Gnss.Quality.Hdop | hdop | dimensionless | Horizontal dilution of precision |
| state.Gnss.Quality.Age | age | seconds | Age of differential correction |

Existing FormGPS code continues to reference legacy fields without modification. All UI labels, displays, and guidance calculations work transparently through the adapter and the shared `_cachedState` snapshot.

## Legacy Processing Bypass

When the backend connection is active, FormGPS disables legacy UDP GPS processing to ensure the backend remains the single source of truth (SourceCode/GPS/Forms/UDPComm.Designer.cs:60-66).

The guard checks `_backendClient?.IsConnected == true` before processing PGN 0xD6 packets. If the backend disconnects or is unavailable, the guard evaluates to false and legacy UDP processing resumes automatically, providing graceful degradation without user intervention.

## UI Data Override Prevention

Legacy timers in FormGPS (tmrWatchdog) historically updated UI labels every 250ms-2s. Guards prevent these updates from overwriting backend data when the connection is active (SourceCode/GPS/Forms/GUI.Designer.cs:262-368).

Labels for speed, fix quality, and Hz update from legacy data only when `_backendClient?.IsConnected != true`. This ensures backend GPS data remains visible even when legacy timers fire.

## Command Dispatching

FormGPS sends CQRS commands to the backend to control the simulator (SourceCode/GPS/Forms/Controls.Designer.cs:2100-2270).

**Available Commands:**

- **Start(position, heading, speed)** - Initialize simulator at given location and motion
- **Stop()** - Halt simulator packet generation
- **SpeedAdjust(delta)** - Increment/decrement speed by delta km/h
- **SpeedZero()** - Set speed to 0 km/h
- **SteeringSet(angle)** - Set steering angle (-40 to +40 degrees)
- **SteeringReset()** - Reset steering to 0 degrees
- **DirectionReverse()** - Reverse heading by 180 degrees
- **Reset()** - Reset simulator to initial state

Commands are wrapped in UpdateSimulatorCommand and sent via `_backendClient.SendCommandAsync()`. The backend MediatR handler processes the command and updates simulator state.

## Disconnection Handling

FormGPS handles backend disconnection gracefully without crashing or freezing.

**Connection Health:** The `_backendClient?.IsConnected` property indicates backend availability. Code can check this before attempting backend-specific operations.

**Error Callback:** OnStateError logs exceptions but does not interrupt application flow. The UI can display warnings while continuing to function.

**Automatic Reconnection:** SignalRBackendClient handles reconnection internally. If the backend restarts, the client automatically reconnects and resumes state reception without user intervention.

## Testing Integration

**Manual Testing Procedure:**

1. Start backend: `dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj`
2. Start frontend: `dotnet run --project SourceCode/GPS/AgOpenGPS.csproj`
3. Verify GPS data flows from backend to UI labels
4. Test simulator controls update speed and steering
5. Stop backend and verify graceful degradation
6. Restart backend and verify automatic reconnection

**Integration Tests:**

- Tests/AgOpenGPS.API.IntegrationTests/StateReceptionTests.cs (3 tests)
- Tests/AgOpenGPS.API.IntegrationTests/SimulatorIntegrationTests.cs (41 tests)

## Migration Roadmap

The adapter pattern is a temporary bridge enabling safe migration.

**Phase 1 (Current):** Backend processes GPS, adapter translates to legacy fields, UI unchanged

**Phase 2 (Future):** Refactor FormGPS to consume ApplicationState directly, remove adapter

**Phase 3 (Future):** Delete legacy UDP GPS processing code entirely

The adapter can be removed when all FormGPS GPS field references are refactored to use ApplicationState properties and legacy UDP GPS processing code is deleted from UDPComm.Designer.cs.

## Troubleshooting

**Backend connection fails:**
- Verify backend is running on localhost:5000
- Check firewall allows localhost connections
- Review FormGPS logs for connection errors

**GPS data not updating:**
- Verify backend logs show "State broadcast" messages
- Check OnStateReceived is being called
- Confirm _backendClient.IsConnected returns true

**UI labels show stale data:**
- Verify backend connection guards in GUI.Designer.cs
- Check tmrWatchdog is not overwriting backend data
- Confirm OnStateReceived calls pn.UpdateFromBackendState

**Simulator commands not working:**
- Verify backend logs show "SimulatorEvent received" messages
- Check SendCommandAsync completes without exception
- Confirm SimulatorService is enabled (Start command sent)
