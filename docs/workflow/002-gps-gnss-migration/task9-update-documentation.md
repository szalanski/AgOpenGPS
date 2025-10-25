# Task 9: Update Documentation

## Goal

Document the GPS/GNSS migration, coordinate transforms, UDP reception, and integration test approach in project documentation.

## Steps

1. Update CLAUDE.md to add GnssService and UdpListenerService to backend project structure
2. Update CLAUDE.md to document coordinate transformation classes (Wgs84, LocalPlane, GeoCoord)
3. Update architecture/03-backend-driven.md with GPS migration as concrete example
4. Document UDP protocol (PGN 0xD6 packet format and fields)
5. Note that AgIO remains external program (NMEA parsing not migrated)
6. Document integration test approach (test before touching FormGPS)
7. Update workflow 002 status to completed
8. Note tmrWatchdog timer restored for UI independence

## Key Points

- Documentation reflects first complete domain migration (GPS/GNSS subsystem)
- Coordinate transform classes are fundamental to GPS processing
- UDP protocol knowledge captured for future reference
- Integration test pattern documented for future migrations
- AgIO remaining external is explicit design decision
- Timer restoration shows UI/backend separation pattern

## Acceptance

- [ ] CLAUDE.md updated with GnssService
- [ ] CLAUDE.md updated with UdpListenerService
- [ ] CLAUDE.md updated with coordinate classes
- [ ] architecture/03-backend-driven.md updated with GPS example
- [ ] UDP protocol documented (PGN 0xD6 format)
- [ ] AgIO external status noted
- [ ] Integration test approach documented
- [ ] Workflow 002 marked completed
- [ ] Documentation builds and renders correctly

## Test

Documentation accurately reflects GPS migration implementation and design decisions.
