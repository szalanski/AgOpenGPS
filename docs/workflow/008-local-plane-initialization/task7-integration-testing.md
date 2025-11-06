# Task 7: Integration Testing

## Goal

Verify all scenarios work correctly with new initialization logic, strong types, and explicit origin control.

## Steps

1. Run full integration test suite (41+ tests expected to pass)
2. Test scenario: Simulator starts, origin defaults to start position
3. Test scenario: Simulator starts with explicit origin different from start position
4. Test scenario: Real GPS initializes local plane on first fix
5. Test scenario: Speed transitions work with Speed value objects
6. Test scenario: Steering changes work with SteeringAngle value objects
7. Test scenario: Position updates work with Wgs84Position value objects
8. Test scenario: VehiclePhysicsService calculations correct with strong types
9. Verify coordinate transformations match expected results
10. Verify logging shows initialization events clearly
11. Add new test case: simulator at position X, origin at position Y (explicit override)
12. Verify frontend-backend coordinate synchronization (if FormGPS runs)

## Key Points

- All 41+ existing tests should pass unchanged
- Add 1-2 new tests for explicit origin scenarios
- Verify coordinate transformation math unchanged (strong types don't affect calculations)
- Check logs for clear initialization messages
- Test both simulator and real GPS initialization paths
- Verify no coordinate jitter or mismatch between backend and frontend
- Performance should be unchanged (value objects are struct wrappers)

## Acceptance

- [ ] All 41+ integration tests pass
- [ ] New test added: explicit origin different from start position
- [ ] Simulator default origin scenario works (origin = start position)
- [ ] Real GPS auto-initialization works (first fix)
- [ ] Speed transitions produce correct results
- [ ] Steering changes produce correct results
- [ ] Position updates produce correct results
- [ ] VehiclePhysicsService calculations match expected values
- [ ] Coordinate transformations are correct
- [ ] Logging clearly shows initialization events
- [ ] No coordinate jitter or synchronization issues
- [ ] Performance is acceptable (no degradation)
