# Task 6: Implement GNSS Service

## Goal

Implement GnssService that unpacks binary GPS packets from AgIO and populates GnssState with all GPS metrics.

## Steps

1. Create GnssService class in AgOpenGPS.Api/Services/ implementing IGnssService
2. Implement ProcessGpsPacket to unpack PGN 0xD6 binary format
3. Extract latitude and longitude from packet (byte positions 5-20)
4. Extract heading (dual antenna and single antenna) from packet
5. Extract speed from packet
6. Extract altitude from packet
7. Extract fix quality, satellites tracked, hdop, age from packet
8. Convert WGS84 coordinates to LocalPlane (easting/northing) using coordinate transforms
9. Calculate gpsHz metric from packet arrival rate
10. Track sentenceCounter for GPS watchdog
11. Implement GetCurrentState to return populated GnssState
12. Implement InitializeLocalPlane to set field origin
13. Add logging for GPS data processing
14. Add validation and error handling
15. Register GnssService in DI container as singleton

## Key Points

- Binary packet format matches UDPComm.Designer.cs unpacking logic (lines 68-164)
- Packet structure: Lat/Lon at bytes 5-20, heading at 21-28, speed at 29-32, etc.
- Must handle float.MaxValue and ushort.MaxValue as "no data" sentinel values
- GpsHz calculated from packet arrival timing (similar to original gpsHz logic)
- SentenceCounter increments on each packet, reset indicates GPS loss
- LocalPlane conversion requires field origin (set via InitializeLocalPlane)
- Service maintains current GPS state (singleton pattern)

## Acceptance

- [ ] GnssService class created implementing IGnssService
- [ ] ProcessGpsPacket unpacks all GPS fields from binary
- [ ] Latitude/longitude extracted correctly
- [ ] Heading (dual and single) extracted correctly
- [ ] Speed, altitude, fix quality extracted correctly
- [ ] Satellites, hdop, age extracted correctly
- [ ] WGS84 converted to LocalPlane coordinates
- [ ] GpsHz calculated from packet rate
- [ ] SentenceCounter tracked
- [ ] GetCurrentState returns populated GnssState
- [ ] InitializeLocalPlane implemented
- [ ] Logging and error handling added
- [ ] Service registered as singleton in DI
- [ ] AgOpenGPS.Api builds successfully

## Test

Unit tests verify binary unpacking produces correct GPS values from test packets.
