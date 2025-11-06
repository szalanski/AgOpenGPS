# Task 4: Add Explicit LocalPlaneOrigin Parameter

## Goal

Add optional LocalPlaneOrigin parameter to SimulatorStartData so clients can specify coordinate origin independently from simulator start position.

## Steps

1. Add LocalPlaneOrigin property to SimulatorStartData record (Wgs84Position? with default null)
2. Add GetEffectiveOrigin() helper method to SimulatorStartData (returns LocalPlaneOrigin ?? Position)
3. Update SimulatorEvent.Start() factory method to accept optional localPlaneOrigin parameter
4. Update SimulatorService.ProcessEvent(Start) to use GetEffectiveOrigin() instead of Position
5. Update logging in ProcessEvent(Start) to indicate explicit vs default origin
6. Verify backward compatibility (existing code with no LocalPlaneOrigin parameter still works)
7. Document usage pattern in code comments

## Key Points

- LocalPlaneOrigin is optional with null default (backward compatible)
- GetEffectiveOrigin() provides clean fallback logic (origin ?? position)
- Most common case: origin = position (client omits parameter)
- Field loading case: origin = Field.txt StartFix (client provides parameter)
- Logging should clearly show which origin is used
- No breaking changes (all existing call sites work unchanged)

## Acceptance

- [ ] SimulatorStartData has LocalPlaneOrigin property (Wgs84Position? with null default)
- [ ] SimulatorStartData has GetEffectiveOrigin() helper method
- [ ] SimulatorEvent.Start() accepts optional localPlaneOrigin parameter
- [ ] SimulatorService.ProcessEvent(Start) uses GetEffectiveOrigin()
- [ ] Logging shows "explicit origin" or "default origin (start position)"
- [ ] Existing tests pass without modification (backward compatibility)
- [ ] Code comments document usage pattern
