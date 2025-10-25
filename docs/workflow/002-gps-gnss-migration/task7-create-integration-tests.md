# Task 7: Create Integration Tests

## Goal

Verify backend correctly receives UDP packets, unpacks GPS data, and broadcasts GnssState via SignalR before connecting FormGPS.

## Steps

1. Create or extend integration test project for backend
2. Create test to send mock UDP packets to backend port
3. Create test to verify binary unpacking (lat/lon, heading, speed extracted correctly)
4. Create test to verify coordinate transformation (Wgs84 → LocalPlane conversion works)
5. Create test to verify GnssState population (all fields populated from packet)
6. Create test to verify SignalR broadcasting (ApplicationState.Gnss received by test client)
7. Create test to verify packet rate tracking (gpsHz calculated correctly from timing)
8. Create test to verify sentence counter watchdog (increments and resets)
9. Run all tests and verify they pass

## Key Points

- Tests prove backend GPS works before touching FormGPS (critical safety step)
- Integration tests use real UDP sockets and SignalR connections
- Mock binary packets should match AgIO's PGN 0xD6 format
- Tests verify math matches original UDPComm.Designer.cs behavior
- GpsHz test requires time-based packet sending
- All tests must pass before proceeding to task 8 (FormGPS changes)

## Acceptance

- [ ] Integration test project exists and configured
- [ ] Test sends UDP packets to backend
- [ ] Test verifies binary unpacking correctness
- [ ] Test verifies coordinate transformations
- [ ] Test verifies GnssState population
- [ ] Test verifies SignalR broadcasting
- [ ] Test verifies gpsHz calculation
- [ ] Test verifies sentence counter
- [ ] All integration tests pass
- [ ] Tests cover critical GPS functionality

## Test

All integration tests pass - backend receives UDP, processes GPS, broadcasts state correctly.
