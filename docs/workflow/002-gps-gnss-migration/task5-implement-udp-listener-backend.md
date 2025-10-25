# Task 5: Implement UDP Listener in Backend

## Goal

Backend listens for UDP packets from AgIO on configured port and forwards GPS packets to GnssService.

## Steps

1. Create UdpListenerService class in AgOpenGPS.Api/Services/ folder
2. Implement IHostedService for background UDP listening
3. Configure UDP port (default 9999 - matches AgIO output)
4. Implement packet reception loop (async UDP socket)
5. Parse PGN packet header to identify packet types
6. Forward 0xD6 packets (GPS data) to IGnssService
7. Handle 0xD3 packets (IMU data) - log for now, process in future
8. Handle 0xD4 packets (disconnect) - log for now
9. Add configuration for UDP port (appsettings.json)
10. Add logging for packet reception and errors
11. Register UdpListenerService in DI container
12. Build and test UDP reception

## Key Points

- UDP port 9999 is AgIO's default output port
- PGN packet format: header identifies packet type (0xD6 for GPS)
- Service runs as background hosted service (starts with backend)
- Must handle packet reception on background thread
- Forward GPS packets to GnssService for processing
- Log other packet types for future migration (IMU, disconnect)
- Should be configurable via appsettings.json

## Acceptance

- [ ] UdpListenerService created implementing IHostedService
- [ ] UDP socket listening on port 9999
- [ ] Packet reception loop implemented
- [ ] PGN header parsing identifies packet types
- [ ] GPS packets (0xD6) forwarded to IGnssService
- [ ] Other packet types logged
- [ ] Configuration added for UDP port
- [ ] Logging added for packet reception
- [ ] Service registered in DI container
- [ ] AgOpenGPS.Api builds successfully

## Test

Backend receives UDP packets from AgIO and logs packet reception.
