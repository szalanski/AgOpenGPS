# Task 4: Create GNSS Service Interface

## Goal

Define backend service contract (IGnssService) for GPS processing with methods for packet processing and state retrieval.

## Steps

1. Create IGnssService interface in AgOpenGPS.Api/Abstractions/ folder
2. Define ProcessGpsPacket method (accepts binary packet data)
3. Define GetCurrentState method (returns GnssState)
4. Define InitializeLocalPlane method (sets coordinate origin)
5. Add XML documentation explaining service purpose and methods
6. Plan dependency injection registration strategy

## Key Points

- IGnssService abstracts GPS processing (enables testing and future transport changes)
- ProcessGpsPacket handles incoming binary UDP packets from AgIO
- GetCurrentState returns latest calculated GPS state for broadcasting
- InitializeLocalPlane sets the field origin for coordinate transformations
- Service will be singleton (one instance tracking GPS state)
- Follows backend abstraction pattern (like IStatePublisher)

## Acceptance

- [ ] IGnssService interface created in Abstractions folder
- [ ] ProcessGpsPacket method defined
- [ ] GetCurrentState method defined
- [ ] InitializeLocalPlane method defined
- [ ] XML documentation added
- [ ] Interface follows backend abstraction patterns
- [ ] AgOpenGPS.Api builds successfully

## Test

Interface compiles and can be implemented by concrete classes.
