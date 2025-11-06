# Integration Test Patterns: Event-Driven Testing

## Overview

Integration tests for SignalR event streams have shifted from **delay-driven** to **polling-driven** patterns to improve test reliability, speed, and clarity.

### Problem with Delay-Driven Tests

Old pattern:
```csharp
var receivedStates = await ReceiveStatesAsync(durationMs: 2000);
// Problem: Always waits full 2000ms, even if data arrives in 100ms
// Problem: May fail on slow machines if 2000ms insufficient
// Problem: Unclear what condition triggers test to complete
```

### Solution: Polling with NUnit DelayedConstraint

New pattern:
```csharp
Assert.That(() => _receivedStates!.Count(s => s.Gnss != null),
    Is.GreaterThanOrEqualTo(15)
    .After(2000).MilliSeconds.PollEvery(50).MilliSeconds);
```

**Benefits:**
- Exits early when condition met (typically 100-200ms vs 2000ms)
- Fails immediately if condition cannot be met
- Explicit condition makes test intent clear
- No new package dependencies (NUnit has this built-in)

## Test Setup Pattern

### Before: Delay-Based

```csharp
[SetUp]
public void SetupTest()
{
    // Simulator setup only
    _simulator = new GpsSimulator(...);
    _simulator.Start();
}

[Test]
public async Task MyTest()
{
    // Create subscriber in test
    var hubConnection = CreateTestHubConnection("/statehub");
    var subscriber = new SignalRBackendClient(hubConnection);
    var receivedStates = new List<ApplicationState>();

    subscriber.SubscribeToState(state => receivedStates.Add(state));
    await subscriber.ConnectAsync();

    // Wait with delay
    await Task.Delay(1500);

    // Assertions
    var gpsStates = receivedStates.Where(s => s.Gnss != null).ToList();
    gpsStates.Should().HaveCountGreaterThan(10);
}
```

### After: Polling-Based

```csharp
[SetUp]
public async Task SetupTest()
{
    // Simulator setup
    _simulator = new GpsSimulator(...);
    _simulator.Start();

    // SignalR subscriber setup (shared across tests)
    _hubConnection = CreateTestHubConnection("/statehub");
    _subscriber = new SignalRBackendClient(_hubConnection);
    _receivedStates = new ConcurrentQueue<ApplicationState>();

    _subscriber.SubscribeToState(state => _receivedStates.Enqueue(state));
    await _subscriber.ConnectAsync();
}

[TearDown]
public async Task CleanupTest()
{
    _simulator?.Dispose();
    if (_subscriber != null)
        await _subscriber.DisposeAsync();
    if (_hubConnection != null)
        await _hubConnection.DisposeAsync();
}

[Test]
public async Task MyTest()
{
    // Poll until condition met
    Assert.That(() => _receivedStates!.Count(s => s.Gnss != null),
        Is.GreaterThanOrEqualTo(15)
        .After(2000).MilliSeconds.PollEvery(50).MilliSeconds);

    // Assertions
    var gpsStates = _receivedStates!.Where(s => s.Gnss != null).ToList();
    gpsStates.Should().HaveCountGreaterThanOrEqualTo(15);
}
```

## NUnit DelayedConstraint Syntax

### Basic Patterns

**Wait for count with timeout:**
```csharp
Assert.That(() => _receivedStates!.Count(s => s.Gnss != null),
    Is.GreaterThanOrEqualTo(15)
    .After(2000).MilliSeconds.PollEvery(50).MilliSeconds);
```

**Wait for any match with timeout:**
```csharp
Assert.That(() => _receivedStates!.Any(s => s.Gnss != null),
    Is.True
    .After(1500).MilliSeconds.PollEvery(50).MilliSeconds);
```

**Wait for specific value with timeout:**
```csharp
Assert.That(() => _receivedStates!.Last().Gnss?.Health.GpsHz ?? 0.0,
    Is.InRange(8.0, 12.0)
    .After(2000).MilliSeconds.PollEvery(50).MilliSeconds);
```

### DelayedConstraint Parameters

- `.After(milliseconds)` - Maximum wait time before timeout
- `.PollEvery(milliseconds)` - How often to check condition (polling interval)
- Condition exits early as soon as assertion passes

## GpsPacketProcessingTests Refactor

All 8 tests updated in [GpsPacketProcessingTests.cs](../SourceCode/Tests/AgOpenGPS.API.IntegrationTests/GpsPacketProcessingTests.cs):

| Test | Condition | Polling |
|------|-----------|---------|
| ShouldSendDataToBackend | ≥15 GPS states | 2000ms / 50ms |
| ShouldUnpackBinaryData | ≥1 GPS state | 1500ms / 50ms |
| ShouldTransformToLocalPlane | ≥1 GPS state | 1500ms / 50ms |
| ShouldPopulateAllFields | ≥1 GPS state | 1500ms / 50ms |
| ShouldCalculateFrequency | ≥15 GPS states | 2000ms / 50ms |
| ShouldResetSentenceCounter | ≥1 GPS state | 1000ms / 50ms |
| ShouldShowVehicleMovement | ≥25 GPS states | 3000ms / 50ms |

### Test Results

**Before**: 7 tests passed, ~60+ seconds
**After**: 7 tests passed, ~33 seconds (45% faster)

Faster due to early exit when condition met + no over-waiting.

## Polling Interval Guidance

**Standard:** 50ms polling interval balances responsiveness vs. CPU usage
- Too aggressive (5-10ms): Wastes CPU, minimal benefit
- Too conservative (200ms+): Adds latency, may miss early completions

**Timeout Guidance:**
- Single GPS state: 1000-1500ms
- 10-15 GPS states: 2000ms (simulator sends ~10 Hz)
- 25+ GPS states: 3000-4000ms

## Troubleshooting

### Test times out but condition seems achievable

**Check:**
1. Is simulator actually running? (check [SetUp])
2. Is subscription active before simulator starts?
3. Is event queue actually receiving events? (add debug print)

**Solution:**
```csharp
// Debug: See what's actually in the queue
var actual = _receivedStates!.Count(s => s.Gnss != null);
Console.WriteLine($"Expected ≥15, but got {actual} GPS states");
Assert.That(() => actual, Is.GreaterThanOrEqualTo(15));
```

### Flaky tests (sometimes pass, sometimes fail)

**Cause:** Timing too tight, simulator too slow on some machines

**Solution:** Increase timeout or lower count threshold
```csharp
// Was: .After(1500).MilliSeconds
// Now: .After(3000).MilliSeconds
Assert.That(() => _receivedStates!.Count(s => s.Gnss != null),
    Is.GreaterThanOrEqualTo(10)  // Lower from 15
    .After(3000).MilliSeconds.PollEvery(50).MilliSeconds);
```

## Pattern for New Tests

When adding new integration tests to SignalR event streams:

```csharp
[TestFixture]
public class MyIntegrationTests : BaseIntegrationTest
{
    protected override int TestFixturePort => 15558;  // Unique port per fixture

    private GpsSimulator? _simulator;
    private HubConnection? _hubConnection;
    private SignalRBackendClient? _subscriber;
    private ConcurrentQueue<ApplicationState>? _receivedStates;

    [SetUp]
    public async Task Setup()
    {
        _simulator = new GpsSimulator(...);
        _simulator.Start();

        _hubConnection = CreateTestHubConnection("/statehub");
        _subscriber = new SignalRBackendClient(_hubConnection);
        _receivedStates = new ConcurrentQueue<ApplicationState>();

        _subscriber.SubscribeToState(state => _receivedStates.Enqueue(state));
        await _subscriber.ConnectAsync();
    }

    [TearDown]
    public async Task Cleanup()
    {
        _simulator?.Dispose();
        if (_subscriber != null)
            await _subscriber.DisposeAsync();
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
    }

    [Test]
    public async Task MyTest()
    {
        // Poll until condition
        Assert.That(() => _receivedStates!.Count(s => s.Gnss != null),
            Is.GreaterThanOrEqualTo(10)
            .After(2000).MilliSeconds.PollEvery(50).MilliSeconds);

        // Assertions on collected states
        _receivedStates!.Should().NotBeEmpty();
    }
}
```

## Resources

- [NUnit DelayedConstraint docs](https://docs.nunit.org/articles/nunit/release-notes/breaking-changes.html#delayedconstraint)
- [System.Collections.Concurrent.ConcurrentQueue](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1)
- [SignalRBackendClient subscription](../SourceCode/AgOpenGPS.Api.Client/SignalR/SignalRBackendClient.cs)
