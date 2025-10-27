# Task 2: Update SimulatorEvent Models to Use Value Objects

## Goal

Replace primitive types in SimulatorStartData and SimulatorEvent with strongly-typed value objects to enable type safety and validation at the API boundary.

## Steps

1. Update SimulatorStartData record to use value objects instead of doubles
2. Replace double? Value field in SimulatorEvent with discriminated union or specific typed fields
3. Update all SimulatorEvent factory methods to accept and create value objects
4. Ensure backward compatibility or coordinate breaking change with consumers
5. Update ProcessEvent method in SimulatorService to work with value objects
6. Update command handlers if they deserialize SimulatorEvent from SignalR
7. Run integration tests to verify SimulatorEvent serialization works with value objects

## Key Points

- SimulatorStartData currently has four double parameters (lat, lon, heading, speed)
- Replace with Wgs84Position, Heading, Speed value objects
- SimulatorEvent.Value is double? used for speed and steering - needs type-safe alternative
- Consider adding specific fields (SpeedValue, SteeringValue) or union type
- Factory methods need parameter type updates (e.g., SpeedSet takes Speed not double)
- SignalR must serialize/deserialize value objects correctly
- This is a breaking change to the client API - all callers must update

## Acceptance

- [ ] SimulatorStartData uses Wgs84Position, Heading, Speed instead of doubles
- [ ] SimulatorEvent no longer uses primitive double? for values
- [ ] All factory methods accept strongly-typed parameters
- [ ] ProcessEvent in SimulatorService works with new types
- [ ] SignalR serialization/deserialization works correctly
- [ ] Integration tests pass with updated event types
- [ ] No primitive obsession remains in command layer

## Test

Create SimulatorEvent instances using factory methods with value objects, verify they serialize correctly and ProcessEvent handles them properly.
