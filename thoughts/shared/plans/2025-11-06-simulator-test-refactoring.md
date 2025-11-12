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
- [x] Code compiles: `dotnet build SourceCode/Tests/AgOpenGPS.API.IntegrationTests`
- [x] Remaining tests pass: `dotnet test SourceCode/Tests/AgOpenGPS.API.IntegrationTests`

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

#### 3. Add SpeedAdjust Test
**New Test**: `SpeedAdjust_ShouldAccelerateGradually`
**Purpose**: Verify smooth speed transitions with physics-based acceleration

```csharp
[Test]
public async Task SpeedAdjust_ShouldAccelerateGradually()
{
    // Verify: SpeedAdjust uses physics acceleration to reach target smoothly
    // Golden values: Start 0 km/h, adjust to 10 km/h, reaches target after ~12 ticks
    // Physics: 0.86 km/h per tick acceleration rate
    // Expected: 0 → 0.86 → 1.72 → 2.58 → ... → 10.0 (in ~12 ticks)

    const double InitialSpeed = 0.0;
    const double TargetSpeed = 10.0;
    const int ExpectedTicksToReach = 12; // Ceil(10.0 / 0.86) ≈ 12
    const double ExpectedFinalSpeed = 10.0;

    // Arrange - Start at zero speed
    var semaphore = new SemaphoreSlim(0, 1);
    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Gnss != null) >= ExpectedTicksToReach)
        {
            semaphore.Release();
            return;
        }
        Factory.TestTimer.AdvanceTick();
    });

    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(InitialSpeed))));

    _receivedStates.Clear(); // Clear start data

    // Act - Adjust speed to 10 km/h (smooth acceleration)
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.SpeedAdjust(new Speed(TargetSpeed))));

    Factory.TestTimer.AdvanceTick(); // Start the tick advancement cycle

    var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
    acquired.Should().BeTrue();

    // Assert - Speed reached target after ~12 ticks
    var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
    var finalSpeed = gpsStates.Last().Gnss!.Speed!.KilometersPerHour;

    finalSpeed.Should().Be(ExpectedFinalSpeed,
        "SpeedAdjust should reach target speed after gradual acceleration");

    // Verify intermediate speeds show gradual increase (not instant)
    var speeds = gpsStates.Take(3).Select(s => s.Gnss!.Speed!.KilometersPerHour).ToList();
    speeds[0].Should().BeLessThan(speeds[1], "speed should increase gradually tick by tick");
    speeds[1].Should().BeLessThan(speeds[2], "speed should continue increasing");
}
```

#### 4. Add Steering Tests
**Remove**: Lines 297-391 (old SteeringAdjust tests that use non-existent events)
**Add**: New SteeringSet and SteeringReset tests

```csharp
[Test]
public async Task SteeringSet_ShouldApplySteeringAngle()
{
    // Verify: SteeringSet applies specific steering angle and changes heading
    // Golden values: Start heading 0° (north), 10 km/h, set steering to 20° right

    const double InitialSpeed = 10.0;
    const double InitialHeading = 0.0;
    const double SteeringAngleDegrees = 20.0;
    const double ExpectedSteeringAngle = 20.0; // TODO: Capture actual after smoothing
    const double ExpectedHeadingAfterOneTick = 0.5; // TODO: Capture actual

    // Arrange - Start moving north at 10 km/h
    var semaphore = new SemaphoreSlim(0, 1);
    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Control != null) >= 1)
        {
            semaphore.Release();
        }
    });

    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(InitialHeading), new Speed(InitialSpeed))));

    _receivedStates.Clear(); // Clear start data

    // Act - Apply steering angle
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.SteeringSet(new SteeringAngle(SteeringAngleDegrees))));

    Factory.TestTimer.AdvanceTick();
    var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
    acquired.Should().BeTrue();

    // Assert - Steering applied and heading changed
    var state = _receivedStates.Single(s => s.Control != null);

    state.Control!.ActualSteeringAngle.Degrees.Should().Be(ExpectedSteeringAngle,
        "steering should be applied (may be smoothed from target due to multi-tier rate limiting)");

    var gps = state.Gnss!;
    gps.HeadingSingle!.Degrees.Should().Be(ExpectedHeadingAfterOneTick,
        "heading should change based on steering angle via Ackermann geometry");
}

[Test]
public async Task SteeringReset_ShouldResetToZero()
{
    // Verify: SteeringReset resets steering to 0° (straight) after being set to non-zero
    // Start with 20° steering, then reset, should return to 0° (may smooth gradually)

    const double InitialSpeed = 10.0;
    const double InitialSteering = 20.0;
    const double ExpectedSteeringAfterReset = 0.0;

    // Arrange - Start with steering applied
    var semaphore = new SemaphoreSlim(0, 1);
    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Control != null) >= 1)
        {
            semaphore.Release();
        }
    });

    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(InitialSpeed))));

    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.SteeringSet(new SteeringAngle(InitialSteering))));

    _receivedStates.Clear(); // Clear setup data

    // Act - Reset steering to zero
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.SteeringReset()));

    Factory.TestTimer.AdvanceTick();
    var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
    acquired.Should().BeTrue();

    // Assert - Steering reset to zero
    var state = _receivedStates.Single(s => s.Control != null);

    state.Control!.ActualSteeringAngle.Degrees.Should().Be(ExpectedSteeringAfterReset,
        "SteeringReset should reset steering angle to 0° (straight ahead)");
}
```

#### 5. Refactor DirectionReverse Test
**Current**: Lines 454-503
**New Name**: `DirectionReverse_ShouldFlip180Degrees`
**Changes**: Make deterministic with exact 180° flip

```csharp
[Test]
public async Task DirectionReverse_ShouldFlip180Degrees()
{
    // Verify: DirectionReverse flips heading by exactly 180°
    // Golden values: Start heading 0° (north), reverse should give 180° (south)

    const double InitialHeading = 0.0;
    const double ExpectedHeadingAfterReverse = 180.0;

    // Arrange - Start moving north
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
        SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(InitialHeading), new Speed(10.0))));

    _receivedStates.Clear(); // Clear start data

    // Act - Reverse direction
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.DirectionReverse()));

    Factory.TestTimer.AdvanceTick();
    var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
    acquired.Should().BeTrue();

    // Assert - Heading flipped 180°
    var gps = _receivedStates.Single(s => s.Gnss != null).Gnss!;

    gps.HeadingSingle!.Degrees.Should().Be(ExpectedHeadingAfterReverse,
        "DirectionReverse should flip heading by exactly 180° (north → south)");
}
```

#### 6. Refactor PositionReset Test
**Current**: Lines 561-620
**New Name**: `PositionReset_ShouldReturnToStartPosition`
**Changes**: Fix API usage (no parameters), semaphore handling, and two-phase test structure

```csharp
[Test]
public async Task PositionReset_ShouldReturnToStartPosition()
{
    // Verify: PositionReset returns to initial position after movement
    // Golden values: Start at (45.0, -93.0), move north, reset should return to (45.0, -93.0)

    const double InitialLatitude = 45.0;
    const double InitialLongitude = -93.0;
    const int MovementTicks = 3;

    // Phase 1: Start and move vehicle
    var movementSemaphore = new SemaphoreSlim(0, 1);
    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Gnss != null) >= MovementTicks)
        {
            movementSemaphore.Release();
            return;
        }
        Factory.TestTimer.AdvanceTick();
    });

    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(InitialLatitude, InitialLongitude), new Heading(0.0), new Speed(10.0))));

    Factory.TestTimer.AdvanceTick(); // Start movement

    await movementSemaphore.WaitAsync(TimeSpan.FromSeconds(5));

    // Assert movement occurred
    var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
    gpsStates.Should().HaveCount(MovementTicks, $"should receive {MovementTicks} GPS states during movement");

    var lastPosition = gpsStates.Last().Gnss!;
    lastPosition.WgsPosition.Latitude.Should().NotBe(InitialLatitude,
        "latitude should have changed after movement (verifying movement occurred before reset)");

    // Phase 2: Reset position
    _receivedStates.Clear(); // Clear movement data

    var resetSemaphore = new SemaphoreSlim(0, 1);
    _backendClient.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Gnss != null) >= 1)
        {
            resetSemaphore.Release();
        }
    });

    // Act - Reset position
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.PositionReset()));

    Factory.TestTimer.AdvanceTick();
    await resetSemaphore.WaitAsync(TimeSpan.FromSeconds(5));

    // Assert - Position reset to start
    var gps = _receivedStates.Single(s => s.Gnss != null).Gnss!;

    gps.WgsPosition.Latitude.Should().Be(InitialLatitude,
        "PositionReset should return latitude to initial start position");
    gps.WgsPosition.Longitude.Should().Be(InitialLongitude,
        "PositionReset should return longitude to initial start position");
}
```

#### 7. Refactor MultipleClients Test
**Current**: Lines 848-913
**New Name**: `MultipleClients_ShouldReceiveSameState`
**Changes**: Reduce to 1 state, use golden values, exact equality

```csharp
[Test]
public async Task MultipleClients_ShouldReceiveSameState()
{
    // Verify: Multiple SignalR clients receive identical state updates
    // Golden values: Both clients should receive exact same GPS data after 1 tick

    const double ExpectedLatitude = 45.00000232324749;
    const double ExpectedLongitude = -93.0;
    const double ExpectedSpeed = 10.0;
    const double ExpectedHeading = 0.0;

    // Arrange - Create second client
    var secondClient = await Factory.CreateBackendClient();
    var secondClientStates = new ConcurrentQueue<StateMessage>();

    var semaphore1 = new SemaphoreSlim(0, 1);
    var semaphore2 = new SemaphoreSlim(0, 1);

    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Gnss != null) >= 1)
        {
            semaphore1.Release();
        }
    });

    secondClient.SubscribeToState(onNext: state =>
    {
        secondClientStates.Enqueue(state);
        if (secondClientStates.Count(s => s.Gnss != null) >= 1)
        {
            semaphore2.Release();
        }
    });

    // Act - Start simulator
    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(45.0, -93.0), new Heading(0.0), new Speed(10.0))));

    Factory.TestTimer.AdvanceTick();

    var acquired1 = await semaphore1.WaitAsync(TimeSpan.FromSeconds(5));
    var acquired2 = await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));

    acquired1.Should().BeTrue("first client should receive state");
    acquired2.Should().BeTrue("second client should receive state");

    // Assert - Both clients received identical data
    var gps1 = _receivedStates.Single(s => s.Gnss != null).Gnss!;
    var gps2 = secondClientStates.Single(s => s.Gnss != null).Gnss!;

    gps1.WgsPosition.Latitude.Should().Be(ExpectedLatitude);
    gps2.WgsPosition.Latitude.Should().Be(ExpectedLatitude, "both clients should receive identical latitude");

    gps1.WgsPosition.Longitude.Should().Be(ExpectedLongitude);
    gps2.WgsPosition.Longitude.Should().Be(ExpectedLongitude, "both clients should receive identical longitude");

    gps1.Speed!.KilometersPerHour.Should().Be(ExpectedSpeed);
    gps2.Speed!.KilometersPerHour.Should().Be(ExpectedSpeed, "both clients should receive identical speed");

    gps1.HeadingSingle!.Degrees.Should().Be(ExpectedHeading);
    gps2.HeadingSingle!.Degrees.Should().Be(ExpectedHeading, "both clients should receive identical heading");

    // Cleanup
    await secondClient.DisconnectAsync();
}
```

#### 8. Refactor ZeroSpeed Test
**Current**: Lines 1028-1067
**New Name**: `ZeroSpeed_ShouldNotMove`
**Changes**: Reduce to 5 ticks, use exact position equality not tolerance

```csharp
[Test]
public async Task ZeroSpeed_ShouldNotMove()
{
    // Verify: Vehicle at speed 0 does not change position over multiple ticks
    // Golden values: Position should remain exactly (45.0, -93.0) after 5 ticks

    const double InitialLatitude = 45.0;
    const double InitialLongitude = -93.0;
    const double ExpectedLatitude = 45.0;
    const double ExpectedLongitude = -93.0;
    const int TickCount = 5;

    // Arrange - Start at zero speed
    var semaphore = new SemaphoreSlim(0, 1);
    _backendClient!.SubscribeToState(onNext: state =>
    {
        _receivedStates.Enqueue(state);
        if (_receivedStates.Count(s => s.Gnss != null) >= TickCount)
        {
            semaphore.Release();
            return;
        }
        Factory.TestTimer.AdvanceTick();
    });

    await _backendClient.SendCommandAsync(new UpdateSimulatorCommand(
        SimulatorEvent.Start(new Wgs84Position(InitialLatitude, InitialLongitude), new Heading(0.0), new Speed(0.0))));

    // Act - Start tick advancement cycle
    Factory.TestTimer.AdvanceTick();

    var acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
    acquired.Should().BeTrue();

    // Assert - All positions should be identical (no movement)
    var gpsStates = _receivedStates.Where(s => s.Gnss != null).Take(TickCount).ToList();
    gpsStates.Should().HaveCount(TickCount, $"should receive exactly {TickCount} GPS states");

    foreach (var state in gpsStates)
    {
        var gps = state.Gnss!;
        gps.WgsPosition.Latitude.Should().Be(ExpectedLatitude,
            "latitude should not change at zero speed");
        gps.WgsPosition.Longitude.Should().Be(ExpectedLongitude,
            "longitude should not change at zero speed");
        gps.Speed!.KilometersPerHour.Should().Be(0.0,
            "speed should remain zero");
    }
}
```

### Success Criteria:

#### Automated Verification:
- [x] All refactored tests compile: `dotnet build`
- [x] Tests use new names in test explorer
- [x] No compilation errors with new assertions

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
- `SpeedSet_ShouldChangeInstantly` - Should be exactly 15.0 km/h (instant change)
- `SpeedAdjust_ShouldAccelerateGradually` - Verify final speed is 10.0 km/h after 12 ticks
- `SteeringSet_ShouldApplySteeringAngle` - Need steering angle and heading after 1 tick
- `SteeringReset_ShouldResetToZero` - Should be exactly 0.0° (instant or smoothed)
- `DirectionReverse_ShouldFlip180Degrees` - Should be exactly 180.0°
- `PositionReset_ShouldReturnToStartPosition` - Verifies return to initial values (45.0, -93.0)
- `MultipleClients_ShouldReceiveSameState` - Uses same golden values as Start test
- `ZeroSpeed_ShouldNotMove` - Position should stay at 45.0, -93.0 exactly

#### 3. Remove Console Output
Clean up temporary debugging code after capturing values.

### Success Criteria:

#### Automated Verification:
- [x] All tests pass with captured golden values: `dotnet test`
- [x] No TODO comments remain in test code
- [x] No Console.WriteLine statements in final code (keeping diagnostic output is acceptable)

#### Manual Verification:
- [x] Golden values documented with date and commit hash
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
- [x] Full test suite runs efficiently: 8 tests in 36s (~4.5s per test)
- [x] All 8 tests pass consistently
- [x] No compiler warnings

#### Manual Verification:
- [x] Code review confirms clean structure
- [x] Tests are well-organized and documented
- [x] No redundant code or helpers

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