# Task 2: Create GNSS State Model

## Goal

Define strongly-typed GnssState model for ApplicationState with all GPS metrics from AgIO packets.

## Steps

1. Create GnssState class in AgOpenGPS.Api.Client/Models/ folder
2. Add properties: Latitude, Longitude, Altitude (double precision coordinates and altitude)
3. Add properties: Heading, HeadingDual, Speed (movement metrics)
4. Add properties: FixQuality, SatellitesTracked, Hdop, Age (GPS quality metrics)
5. Add properties: GpsHz, SentenceCounter (GPS health metrics)
6. Add properties: Easting, Northing (local plane coordinates)
7. Add Gnss property (type GnssState) to ApplicationState class
8. Add XML documentation for all properties explaining units and meaning
9. Build AgOpenGPS.Api.Client project

## Key Points

- GnssState represents complete GPS data from one packet
- Property types match original CNMEA class fields (double for coordinates, int for quality)
- Easting/Northing are local plane coordinates (not WGS84 lat/lon)
- GpsHz tracks GPS message arrival frequency
- SentenceCounter used for GPS watchdog (loss detection)
- Must be serializable for SignalR broadcasting

## Acceptance

- [ ] GnssState class created in AgOpenGPS.Api.Client/Models/
- [ ] All GPS properties defined with correct types
- [ ] XML documentation added for each property
- [ ] Gnss property added to ApplicationState
- [ ] AgOpenGPS.Api.Client builds successfully
- [ ] Properties accessible from both backend and frontend

## Test

Model builds successfully, properties are accessible in both AgOpenGPS.Api and GPS projects.
