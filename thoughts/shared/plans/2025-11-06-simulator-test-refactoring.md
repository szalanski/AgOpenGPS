# SimulatorUnifiedCommandTests Refactoring Implementation Plan

## Overview

Refactor the SimulatorUnifiedCommandTests to be faster, more deterministic, and focused on actual value. We reviewed all 22 tests and decided to keep/refactor 9 tests while removing 13 redundant or low-value tests.

## Current State Analysis

The existing test suite has 22 tests that:
- Use arbitrary wait counts (5, 10, 15+ states)
- Have vague range-based assertions instead of exact values
- Test infrastructure/implementation details rather than behavior
- Include redundant coverage of the same functionality
- Take too long to run due to excessive tick waits

## Desired End State

A focused test suite with 9 high-value tests that:
- Use minimal tick counts (1-5 ticks max)
- Assert exact golden values for deterministic verification
- Focus on simulator command behavior, not infrastructure
- Complete execution quickly while maintaining coverage
- Catch regressions effectively through precise assertions

### Key Discoveries:
- TestTimer pattern enables synchronous, deterministic testing at `TestSimulatorTimer.cs:9-29`
- Each test gets isolated SignalR connection at `SimulatorUnifiedCommandTests.cs:29-31`
- Simulator generates deterministic output given same inputs (research doc: `2025-11-06-simulator-service-calculations.md`)

## What We're NOT Doing

- Not testing SignalR infrastructure (connection, broadcasting)
- Not testing UDP packet flow or timing
- Not testing multi-tick physics calculations (belongs in unit tests)
- Not testing obvious invariants (e.g., "reverse doesn't change speed")
- Not creating external golden value files (keeping inline)

## Implementation Approach

We'll implement this in 4 phases:
1. Remove redundant/low-value tests
2. Refactor existing tests to be deterministic
3. Capture and verify golden values
4. Final cleanup and optimization

## Phase 1: Remove Redundant Tests

### Overview
Remove 13 tests that provide low value or duplicate coverage.

### Changes Required:

#### 1. Remove Stop Event Test
**File**: `SourceCode/Tests/AgOpenGPS.API.IntegrationTests/SimulatorUnifiedCommandTests.cs`
**Lines to Remove**: 94-128

#### 2. Remove Speed Adjustment Tests
**Lines to Remove**:
- 133-179 (SpeedAdjust_ShouldAccelerate)
- 181-227 (SpeedAdjust_ShouldDecelerate)

#### 3. Remove Direction Maintain Speed Test
**Lines to Remove**: 506-554

#### 4. Remove Complex Scenario Test
**Lines to Remove**: 627-696

#### 5. Remove Architectural Verification Tests
**Lines to Remove**:
- 703-748 (SimulatorService_ShouldSendViaUdp)
- 751-795 (SimulatorPackets_ShouldFlowThroughFullPipeline)
- 798-845 (SimulatorGpsData_ShouldMatchAgIOPacketFormat)

#### 6. Remove Physics Accuracy Tests
**Lines to Remove**:
- 920-962 (SimulatorMovement_ShouldShowRealisticPositionChanges)
- 965-1025 (SimulatorSteering_ShouldCreateCurvedPath)
- 1070-1123 (SpeedChanges_ShouldAffectDistanceTraveled)

#### 7. Remove SignalR Infrastructure Tests
**Lines to Remove**:
- 1130-1147 (SignalRBackendClient_ShouldConnect)
- 1150-1195 (SignalRBackendClient_ShouldReceiveStateUpdates)

### Success Criteria:

#### Automated Verification:
- [ ] Code compiles: `dotnet build SourceCode/Tests/AgOpenGPS.API.IntegrationTests`
- [ ] Remaining tests pass: `dotnet test SourceCode/Tests/AgOpenGPS.API.IntegrationTests`

#### Manual Verification:
- [ ] Verify 13 tests removed from test explorer
- [ ] Confirm test count reduced from 22 to 9

---

## Phase 2: Refactor Core Tests

### Overview
Update the 9 remaining tests with new names, deterministic assertions, and proper structure.

### Changes Required:

#### 1. Rename and Refactor Start Event Test
**File**: `SourceCode/Tests/AgOpenGPS.API.IntegrationTests/SimulatorUnifiedCommandTests.cs`
**Current**: Lines 58-92
**New Name**: `Start_ShouldProduceExpectedFirstGpsState`

```csharp
[Test]
public async Task Start_ShouldProduceExpectedFirstGpsState()
{
    // Verify: First tick after Start command produces exact physics results
    // Golden values captured from simulator on 2025-11-06 (commit: 9f61df14)
    // Physics: Start (45.0, -93.0), Heading: 0°, Speed: 10 km/h (instant)
    // Tick 1: 10 km/h for 93ms = 0.258m north via great circle navigation

    const double ExpectedLatitude = 45.00000232324749;
    const double ExpectedLongitude = -93.0;
    const double ExpectedSpeed = 10.0;
    const double ExpectedHeading = 0.0;

    // Arrange
    var semaphore = new SemaphoreSlim(0, 1);
    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Gnss != null) >= 1)
        {
            semaphore.Release();
        }
    });

    // Act
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

    Factory.TestTimer.AdvanceTick();
    var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
    acquired.Should().BeTrue("should receive GPS data within 5 seconds");

    // Assert
    var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
    gpsStates.Should().HaveCount(1, "should receive exactly 1 GPS state after first tick");

    var gps = gpsStates.Single().Gnss!;

    gps.WgsPosition.Latitude.Should().Be(ExpectedLatitude,
        "latitude after first tick moving 0.258m north at 10 km/h");
    gps.WgsPosition.Longitude.Should().Be(ExpectedLongitude,
        "longitude should not change when moving due north");
    gps.Speed!.KilometersPerHour.Should().Be(ExpectedSpeed,
        "Start command sets speed instantly to 10 km/h");
    gps.HeadingSingle!.Degrees.Should().Be(ExpectedHeading,
        "heading should be 0° (north) as commanded");
}
```

#### 2. Refactor SpeedSet Test
**Current**: Lines 229-272
**New Name**: `SpeedSet_ShouldChangeInstantly`

```csharp
[Test]
public async Task SpeedSet_ShouldChangeInstantly()
{
    // Verify: SpeedSet changes speed instantly to target (no acceleration)
    // Start: 10 km/h → SpeedSet(15) → immediately 15 km/h on first tick

    const double InitialSpeed = 10.0;
    const double TargetSpeed = 15.0;
    const double ExpectedSpeed = 15.0;

    // Arrange - Start at 10 km/h
    var semaphore = new SemaphoreSlim(0, 1);
    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Gnss != null) >= 1)
        {
            semaphore.Release();
        }
    });

    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(InitialSpeed))));

    _receivedStates.Clear(); // Clear start data

    // Act - Set speed instantly to 15 km/h
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.SpeedSet(new Speed(TargetSpeed))));

    Factory.TestTimer.AdvanceTick();
    var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
    acquired.Should().BeTrue();

    // Assert - Speed changed instantly
    var gps = _receivedStates.Single(s => s.Gnss != null).Gnss!;
    gps.Speed!.KilometersPerHour.Should().Be(ExpectedSpeed,
        "SpeedSet should change speed instantly without acceleration");
}
```

#### 3. Replace Steering Tests
**Remove**: Lines 297-391 (old SteeringAdjust tests that use non-existent events)
**Add**: New SteeringSet and SteeringReset tests

```csharp
[Test]
public async Task SteeringSet_ShouldApplySteeringAngle()
{
    // Verify: SteeringSet applies specific steering angle and changes heading
    // Golden values: Start heading 0° (north), 10 km/h, set steering to 20° right

    const double InitialHeading = 0.0;
    const double SteeringAngleDegrees = 20.0;
    const double ExpectedSteeringAngle = 20.0; // TODO: Capture actual
    const double ExpectedHeadingAfterOneTick = 0.5; // TODO: Capture actual

    // [Rest of implementation as designed in review]
}

[Test]
public async Task SteeringReset_ShouldResetToZero()
{
    // Verify: SteeringReset resets steering to 0° (straight)
    const double ExpectedSteeringAfterReset = 0.0;

    // [Rest of implementation as designed in review]
}
```

#### 4. Refactor DirectionReverse Test
**Current**: Lines 454-503
**Changes**: Make deterministic with exact 180° flip

#### 5. Refactor PositionReset Test
**Current**: Lines 561-620
**Changes**: Fix API usage (no parameters) and semaphore handling

#### 6. Refactor MultipleClients Test
**Current**: Lines 848-913
**Changes**: Reduce to 1 state, use golden values, exact equality

#### 7. Refactor ZeroSpeed Test
**Current**: Lines 1028-1067
**Changes**: Reduce to 5 states, use exact equality not tolerance

### Success Criteria:

#### Automated Verification:
- [ ] All refactored tests compile: `dotnet build`
- [ ] Tests use new names in test explorer
- [ ] No compilation errors with new assertions

#### Manual Verification:
- [ ] Review that each test follows new pattern (1-5 ticks max)
- [ ] Verify timeout protection added (5 second timeout)
- [ ] Confirm tests are more readable with clear intent

---

## Phase 3: Capture Golden Values

### Overview
Run tests to capture actual golden values from simulator and update constants.

### Changes Required:

#### 1. Run Tests with Console Output
Add temporary console output to capture values:

```csharp
// Temporary addition for capturing golden values
Console.WriteLine($"Latitude: {gps.WgsPosition.Latitude}");
Console.WriteLine($"Longitude: {gps.WgsPosition.Longitude}");
Console.WriteLine($"Speed: {gps.Speed!.KilometersPerHour}");
Console.WriteLine($"Heading: {gps.HeadingSingle!.Degrees}");
Console.WriteLine($"Steering: {state.Control!.ActualSteeringAngle.Degrees}");
```

#### 2. Update Constants with Captured Values
Run each test and update the TODO placeholders with actual values:
- `Start_ShouldProduceExpectedFirstGpsState` - Already have: 45.00000232324749
- `SpeedSetSmooth_ShouldAccelerateToTarget` - Need final speed after 6 ticks
- `SteeringSet_ShouldApplySteeringAngle` - Need steering angle and heading after 1 tick
- `DirectionReverse_ShouldFlip180Degrees` - Should be exactly 180.0
- `PositionReset_ShouldReturnToStartPosition` - Should be exactly 45.0, -93.0

#### 3. Remove Console Output
Clean up temporary debugging code after capturing values.

### Success Criteria:

#### Automated Verification:
- [ ] All tests pass with captured golden values: `dotnet test`
- [ ] No TODO comments remain in test code
- [ ] No Console.WriteLine statements in final code

#### Manual Verification:
- [ ] Golden values documented with date and commit hash
- [ ] Values are reasonable (e.g., heading 0-360, speeds positive)
- [ ] Tests detect regressions (change a value and verify failure)

---

## Phase 4: Final Optimization

### Overview
Clean up test structure and ensure optimal performance.

### Changes Required:

#### 1. Consolidate Using Statements
Review and remove unused using statements.

#### 2. Update Test Categories
Reorganize remaining tests into logical regions:
- Start/Stop Events (1 test)
- Speed Events (2 tests)
- Steering Events (2 tests)
- Direction/Reset Events (2 tests)
- Multi-Client Tests (1 test)
- Edge Cases (1 test - ZeroSpeed)

#### 3. Add Test Documentation
Ensure each test has clear documentation:
- Purpose comment
- Golden value source
- Expected behavior

### Success Criteria:

#### Automated Verification:
- [ ] Full test suite runs in < 5 seconds: `dotnet test`
- [ ] All 9 tests pass consistently
- [ ] No compiler warnings

#### Manual Verification:
- [ ] Code review confirms clean structure
- [ ] Tests are well-organized and documented
- [ ] No redundant code or helpers

## Testing Strategy

### Regression Testing:
- Change simulator physics and verify tests catch the change
- Modify golden values slightly and confirm test failures
- Run tests multiple times to ensure consistency

### Performance Testing:
- Measure test execution time before and after refactoring
- Target: Full suite completion in < 5 seconds
- Individual test target: < 500ms each

## Migration Notes

- Tests can be migrated incrementally - each phase is independent
- Keep backup of original test file before starting
- If rollback needed, restore from git history
- Golden values are tied to current simulator implementation

## References

- Original test file: `SourceCode/Tests/AgOpenGPS.API.IntegrationTests/SimulatorUnifiedCommandTests.cs`
- Test infrastructure analysis: Task agent research results
- Simulator physics research: `thoughts/shared/research/2025-11-06-simulator-service-calculations.md`
- Review discussion: Current conversation context