# Task 3: Replace Primitives in VehiclePhysicsService

## Goal

Replace primitive type parameters and return types with strong value objects in all VehiclePhysicsService methods.

## Steps

1. Update TransitionSpeed signature to accept (Speed current, Speed target) and return Speed
2. Update SmoothSteeringAngle signature to accept (SteeringAngle current, SteeringAngle target) and return SteeringAngle
3. Update CalculateHeadingChange signature to accept (SteeringAngle angle, Distance stepDistance) and return heading change
4. Update NormalizeHeading signature to accept Heading and return Heading
5. Update CalculateNewPosition signature to accept (Wgs84Position, Heading, Distance) and return Wgs84Position
6. Create Distance value object if needed (or use existing type)
7. Update SimulatorService.Tick() to pass strong types to physics methods
8. Update SimulatorService.Tick() to receive strong types from physics methods
9. Remove primitive conversions from SimulatorService.Tick()
10. Verify all physics calculations still work with value object properties

## Key Points

- VehiclePhysicsService should have zero primitive parameters (all strong types)
- Distance value object may need to be created (check if exists first)
- SimulatorService.Tick() becomes cleaner (no constant wrapping/unwrapping)
- Value object properties provide units (Speed.KilometersPerHour, Heading.Degrees, etc.)
- Great circle navigation math uses Wgs84Position input and output
- Heading normalization works with Heading value object

## Acceptance

- [ ] TransitionSpeed uses Speed parameters and returns Speed
- [ ] SmoothSteeringAngle uses SteeringAngle parameters and returns SteeringAngle
- [ ] CalculateHeadingChange uses SteeringAngle and Distance parameters
- [ ] NormalizeHeading uses Heading parameter and returns Heading
- [ ] CalculateNewPosition uses Wgs84Position, Heading, Distance and returns Wgs84Position
- [ ] SimulatorService.Tick() passes strong types to all physics methods
- [ ] No primitive conversions in SimulatorService.Tick()
- [ ] Code compiles and physics calculations remain correct
