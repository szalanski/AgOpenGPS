# Task 3: Refactor SimulatorService Internal State to Use Value Objects

## Goal

Replace all primitive fields in SimulatorService with strongly-typed value objects, eliminating primitive obsession and enabling compile-time invariant enforcement.

## Steps

1. Replace latitude/longitude doubles with Wgs84Position field
2. Replace heading double/radians with Heading value object
3. Replace speed doubles (current and target) with Speed value objects
4. Replace steering angle doubles with SteeringAngle value objects
5. Update all public methods (Start, SetSpeed, SetSteering, etc.) to use value objects
6. Refactor Tick method to work with value objects instead of primitives
7. Update SimulatorPhysics static methods to accept value objects as parameters
8. Remove primitive conversion code that's no longer needed
9. Verify thread safety with new value object fields

## Key Points

- Current state has 7+ primitive double fields (lat, lon, heading, speed, targetSpeed, steerAngle, steerAngleAve)
- Replace with fewer, richer value object fields
- Initial position tracking (for Reset) should use Wgs84Position
- Value objects are immutable - use replacement not mutation
- Lock synchronization still needed for thread safety
- Methods like TransitionSpeed should return new Speed, not mutate
- Consider if smoothing logic (steerAngleAve) belongs in value object or service

## Acceptance

- [ ] No primitive double fields remain for domain concepts (position, heading, speed, steering)
- [ ] All public API methods use value objects
- [ ] Tick method works with value objects internally
- [ ] Thread safety maintained with proper locking
- [ ] Code is more readable with explicit types
- [ ] Invalid states impossible at compile time
- [ ] All existing functionality preserved

## Test

Run existing integration tests to verify functionality unchanged - tests should pass with internal value object refactoring.
