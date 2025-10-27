# Task 1: Create SteeringAngle Value Object

## Goal

Create a SteeringAngle value object that enforces valid steering ranges and provides type safety for steering operations in the simulator.

## Steps

1. Create SteeringAngle struct in AgOpenGPS.Api.Client.Models namespace
2. Add validation for steering angle range (reasonable limits for agricultural vehicle steering)
3. Implement value object equality semantics (Equals, GetHashCode, operators)
4. Add comparison operators (greater than, less than) for steering comparisons
5. Implement smoothing behavior or utility methods if needed for gradual steering transitions
6. Add XML documentation describing steering angle semantics (positive = right turn, negative = left turn)
7. Add unit tests for validation, equality, and edge cases

## Key Points

- Study existing value objects (Speed, Heading, Altitude) for consistent pattern
- Steering angle typically ranges from -45 to +45 degrees for agricultural vehicles
- Consider whether SteeringAngle should store degrees or radians (simulator uses degrees externally)
- Ensure struct is immutable with init-only setters
- Consider adding zero/center constant for readability

## Acceptance

- [ ] SteeringAngle value object created in AgOpenGPS.Api.Client.Models
- [ ] Validation prevents invalid steering angles (enforce reasonable limits)
- [ ] Immutable struct with proper equality semantics
- [ ] XML documentation explains meaning and valid range
- [ ] Unit tests cover validation, equality, and edge cases
- [ ] Consistent with existing value object patterns (Speed, Heading)

## Test

Create SteeringAngle instances with valid and invalid values, verify validation works and equality comparisons behave correctly.
