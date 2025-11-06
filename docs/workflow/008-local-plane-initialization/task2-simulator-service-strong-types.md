# Task 2: Replace Primitives in SimulatorService

## Goal

Replace primitive type parameters with strong value objects in all SimulatorService public methods.

## Steps

1. Update Start method signature to accept (Wgs84Position, Heading, Speed) instead of primitives
2. Update SetSpeed method to accept Speed parameter instead of double
3. Update AdjustSpeed method to accept Speed delta instead of double
4. Update SetSteering method to accept SteeringAngle instead of double
5. Update ResetPosition method to accept Wgs84Position instead of (double lat, double lon)
6. Update ProcessEvent(Start) to pass strong types directly to Start method
7. Update ProcessEvent(SpeedSet/SpeedSetSmooth) to pass Speed object
8. Update ProcessEvent(SpeedAdjust) to create Speed delta object
9. Update ProcessEvent(SteeringSet) to pass SteeringAngle object
10. Update ProcessEvent(PositionReset) to pass Wgs84Position object
11. Remove primitive extraction from ProcessEvent switch cases
12. Update internal logic to work with value objects

## Key Points

- All public methods should use value objects (no double lat, double lon, double speed, etc.)
- ProcessEvent should pass value objects directly from event data
- Constructor default values can remain primitive (converted to value objects immediately)
- Value objects already exist (Wgs84Position, Heading, Speed, SteeringAngle)
- This change is internal to SimulatorService (call sites updated in Task 6)

## Acceptance

- [ ] Start method signature uses (Wgs84Position, Heading, Speed)
- [ ] SetSpeed method signature uses Speed parameter
- [ ] AdjustSpeed method signature uses Speed delta
- [ ] SetSteering method signature uses SteeringAngle parameter
- [ ] ResetPosition method signature uses Wgs84Position parameter
- [ ] ProcessEvent passes strong types to all methods
- [ ] No primitive extraction in ProcessEvent (value objects passed directly)
- [ ] Code compiles (call sites in SimulatorService itself work)
