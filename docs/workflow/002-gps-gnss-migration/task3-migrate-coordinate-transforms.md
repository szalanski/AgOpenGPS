# Task 3: Migrate Coordinate Transforms

## Goal

Port Wgs84, LocalPlane, and GeoCoord coordinate transformation classes from AgOpenGPS.Core to backend for GPS position calculations.

## Steps

1. Create Models folder in AgOpenGPS.Api project
2. Port Wgs84 class (WGS84 latitude/longitude representation)
3. Port GeoCoord class (local plane easting/northing representation)
4. Port LocalPlane class (WGS84 to local ENU coordinate conversion)
5. Remove dependencies on AgOpenGPS.Core (make classes standalone)
6. Update to .NET 8 compatibility (remove .NET Framework dependencies)
7. Add unit tests for coordinate transformations
8. Build AgOpenGPS.Api project

## Key Points

- These classes enable GPS coordinate conversion: WGS84 (lat/lon) → LocalPlane → GeoCoord (easting/northing)
- Backend needs these for processing AgIO packets (which contain WGS84 coordinates)
- Must work identically to original AgOpenGPS.Core versions
- LocalPlane uses origin point and earth curvature calculations
- GeoCoord represents meters from local origin (easier for field operations)
- Unit tests verify math matches original behavior

## Acceptance

- [ ] Wgs84 class ported to AgOpenGPS.Api/Models/
- [ ] GeoCoord class ported to AgOpenGPS.Api/Models/
- [ ] LocalPlane class ported to AgOpenGPS.Api/Models/
- [ ] No dependencies on AgOpenGPS.Core
- [ ] .NET 8 compatible (no .NET Framework dependencies)
- [ ] Unit tests created for coordinate conversions
- [ ] Tests verify results match original implementation
- [ ] AgOpenGPS.Api builds successfully

## Test

Unit tests pass - coordinate transformations produce identical results to original AgOpenGPS.Core classes.
