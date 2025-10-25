# GPS/GNSS Migration to Backend

## Goal

Restore tmrWatchdog timer for UI-only updates while migrating complete GPS/GNSS processing to backend. Backend owns UDP packet reception from AgIO, binary protocol unpacking, coordinate transformations, and GPS state calculations - broadcasting all GPS data via ApplicationState.Gnss property.

## Current State

Workflow 001 completed: Backend broadcasts ApplicationState (only Timestamp property) at 4 Hz (250ms). FormGPS connects via SignalR and OnStateReceived() calls ProcessApplicationTick() on each state update. tmrWatchdog timer completely deleted - backend now drives the tick. ProcessApplicationTick() contains all GPS parsing, vehicle state calculations, and UI updates. Backend has no business logic yet - only infrastructure (ApplicationOrchestrator, StateHub, SignalR).

FormGPS receives UDP packets from AgIO in UDPComm.Designer.cs, unpacks binary GPS data (PGN 0xD6), populates CNMEA fields, and uses them throughout Position.designer.cs for vehicle calculations.

## Target State

tmrWatchdog timer restored (250ms) handling only UI updates (labels, colors, panels). Backend receives UDP packets from AgIO, unpacks binary protocol (PGN 0xD6), performs coordinate transformations (Wgs84 → LocalPlane → GeoCoord), and calculates complete GPS state. ApplicationState.Gnss contains all GPS data (position, heading, speed, altitude, fix quality, age, satellites, hdop, gpsHz). FormGPS displays GPS data from backend state. Clear separation: backend owns GPS/GNSS business logic, frontend only renders UI. Integration tests verify backend GPS processing before connecting FormGPS.

## Why

- **First complete domain migration**: Proves strangler fig pattern by migrating entire GPS subsystem (not just properties) - establishes template for future domain migrations (Guidance, Section Control)
- **Backend becomes headless-capable**: GPS processing runs independently of UI, enabling backend-only testing and deployment scenarios
- **Clear separation achieved**: UI timer for rendering, backend for all GPS business logic - demonstrates thin UI architecture goal

## What This Is NOT

- **NOT migrating Vehicle state calculations yet**: CVehicle position adjustments, antenna pivot corrections, roll corrections, turn detection stay in FormGPS - only raw GPS processing moves
- **NOT migrating Guidance, Section Control, or Boundaries**: Only GPS/GNSS subsystem - guidance calculations (AB lines, curves) come in next workflow
- **NOT migrating AgIO's NMEA parsing**: AgIO remains external program doing NMEA→binary conversion - only FormGPS's UDP reception and unpacking migrates to backend

## Migration Path

### Phase 1: Restore UI Independence
1. Restore tmrWatchdog timer for UI updates
2. Stop calling ProcessApplicationTick from backend state updates
3. Verify UI works independently

### Phase 2: Backend GPS Infrastructure
1. Create GnssState model (Lat, Lon, Heading, Speed, Altitude, Fix, Age, Satellites, Hdop, GpsHz)
2. Port coordinate transformation classes (Wgs84, LocalPlane, GeoCoord) to backend
3. Define IGnssService interface

### Phase 3: Backend Receives GPS Data
1. Implement UdpListenerService in backend (listen for AgIO packets)
2. Implement GnssService (unpack binary protocol, transform coordinates, populate state)
3. Wire into ApplicationOrchestrator to broadcast ApplicationState.Gnss

### Phase 4: Verify Backend Works (Integration Tests First!)
1. Create integration tests for UDP reception
2. Test binary unpacking and coordinate transforms
3. Test GnssState population and SignalR broadcasting
4. Verify packet rate tracking (gpsHz)
5. All tests pass before touching FormGPS

### Phase 5: Frontend Uses Backend GPS
1. FormGPS receives ApplicationState.Gnss from backend
2. Remove/disable local UDP processing in UDPComm.Designer.cs
3. Update Position.designer.cs to use backend state
4. Verify UI displays match previous behavior

## Tasks

1. [task1-restore-tmrwatchdog-timer.md](task1-restore-tmrwatchdog-timer.md) - Revert workflow 001 timer deletion
2. [task2-create-gnss-state-model.md](task2-create-gnss-state-model.md) - Define GnssState and add to ApplicationState
3. [task3-migrate-coordinate-transforms.md](task3-migrate-coordinate-transforms.md) - Port Wgs84, LocalPlane, GeoCoord to backend
4. [task4-create-gnss-service-interface.md](task4-create-gnss-service-interface.md) - Define IGnssService contract
5. [task5-implement-udp-listener-backend.md](task5-implement-udp-listener-backend.md) - Backend receives UDP from AgIO
6. [task6-implement-gnss-service.md](task6-implement-gnss-service.md) - Unpack binary protocol, populate GPS state
7. [task7-create-integration-tests.md](task7-create-integration-tests.md) - Verify backend GPS works before touching FormGPS
8. [task8-frontend-use-backend-gnss.md](task8-frontend-use-backend-gnss.md) - FormGPS consumes backend GPS state
9. [task9-update-documentation.md](task9-update-documentation.md) - Document GPS migration

## Success Criteria

- [ ] tmrWatchdog timer restored for UI updates (250ms)
- [ ] Backend receives UDP packets from AgIO on port 9999
- [ ] Backend unpacks PGN 0xD6 binary protocol correctly
- [ ] Coordinate transformations work (Wgs84 → LocalPlane → GeoCoord)
- [ ] ApplicationState.Gnss populated with all GPS metrics
- [ ] Integration tests pass (UDP, unpacking, transforms, broadcasting)
- [ ] FormGPS displays GPS data from backend state
- [ ] UI behavior matches previous implementation
- [ ] AgIO remains unchanged (external program)
- [ ] CNMEA class removed or kept as thin adapter
- [ ] Documentation updated with GPS migration details
