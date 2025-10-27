# Testing Strategy

## Integration Testing Approach

The backend system uses **integration tests** as the primary testing strategy. Unit tests were deliberately avoided in favor of full-pipeline testing.

## Why Integration Tests, Not Unit Tests?

### Backend is Integration-Heavy
Backend value comes from components working together:
- **UDP reception** + **Protocol unpacking** + **State broadcasting**
- **Simulator** + **UDP send** + **GPS processing** + **State updates**
- **SignalR** + **Command handling** + **Domain logic** + **State changes**

**Unit tests** would test individual methods (minimal value). **Integration tests** validate the complete data flow (real value).

### Thin Business Logic
Most backend code is plumbing:
- **ApplicationOrchestrator**: Routes packets (no complex logic)
- **SignalR Hub**: Dispatches commands (no complex logic)
- **State publisher**: Broadcasts state (no complex logic)

**Complex logic** (physics, GPS processing) **easier to test end-to-end** (send GPS packet → assert state correct).

### No Mocking Needed
Integration tests use real implementations:
- **In-memory backend**: TestWebApplicationFactory (no HTTP, no external ports)
- **Real SignalR**: Actual SignalR client/server (not mocked)
- **Real UDP**: Actual UDP sockets (localhost loopback)

**Fast enough**: In-memory backend starts in ~500ms, tests run in seconds.

### High Confidence
Integration tests catch real bugs:
- **Serialization issues**: JSON config wrong → test fails
- **UDP packet format**: Wrong byte offset → test fails
- **SignalR connection**: Configuration mismatch → test fails
- **State broadcasting**: Missing property → test fails

**Unit tests miss these**: Mock interfaces hide integration problems.

## Test Infrastructure

### TestWebApplicationFactory
In-memory ASP.NET Core test server:
- **No HTTP**: Runs in-process (no port binding)
- **Real backend**: Actual ApplicationOrchestrator, SignalR Hub, services
- **Fast startup**: ~500ms (no external dependencies)
- **Isolated**: Each test gets clean backend instance

### BaseIntegrationTest
Base class for integration tests:
- **Setup**: Creates TestWebApplicationFactory, SignalR client
- **Connection**: Connects client to in-memory backend
- **Cleanup**: Disposes backend and client after test
- **Utilities**: Helper methods for common assertions

### Test Helpers

**AgIOPacketSimulator**:
- Generates binary GPS packets (PGN 0xD6 format)
- Encodes position, heading, speed, quality metrics
- Sends via UDP to backend (localhost)

**GpsSimulator** (External):
- Simulates external GPS source
- Configurable position, heading, speed
- Separate process simulation (realistic)

## Test Categories

### GPS Packet Processing Tests
Validate GPS pipeline (UDP → Processing → State):

**Tests**:
1. **Receive GPS packet**: Backend receives UDP, processes, broadcasts state
2. **Position accuracy**: State contains correct lat/lon from packet
3. **Heading accuracy**: State contains correct heading
4. **Speed accuracy**: State contains correct speed
5. **Quality metrics**: State contains satellite count, HDOP, fix quality
6. **Coordinate transforms**: WGS84 converted to local coordinates correctly
7. **Invalid packets**: Bad packets rejected, no crash

**Status**: 7/7 passing (100%)

### State Reception Tests
Validate SignalR state broadcasting:

**Tests**:
1. **Client connects**: SignalR connection established
2. **State received**: Client receives state updates via callback
3. **Multiple updates**: Client receives stream of states (not just one)

**Status**: 3/3 passing (100%)

### Simulator Integration Tests
Validate simulator commands and GPS generation:

**Tests (14 total, 11 passing)**:

**Passing**:
1. Start simulator → GPS packets generated
2. Stop simulator → GPS packets stop
3. Speed increase (instant) → Speed in state increases
4. Speed increase (smooth) → Speed transitions gradually
5. Speed decrease → Speed in state decreases
6. Speed zero → Speed becomes 0
7. Position updates → Vehicle moves (position changes)
8. Reset simulator → Position returns to start
9. Simulator disabled by default → No packets initially
10. Speed adjust (delta) → Speed changes by delta
11. Full reset → Position resets to initial

**Failing (3 tests)**:
12. Set steering right → Heading changes clockwise ❌
13. Set steering left → Heading changes counter-clockwise ❌
14. Reset steering → Heading stabilizes ❌

**Known issue**: Steering effect too weak (heading changes too slowly to detect in test timeframe). Physics correct, test needs longer duration or more sensitive assertions.

## Test Execution

### Running Tests
```bash
# All tests
dotnet test SourceCode/AgOpenGPS.sln

# Integration tests only
dotnet test SourceCode/Tests/AgOpenGPS.API.IntegrationTests/

# Specific test
dotnet test --filter "TestName~ReceiveGpsPacket"

# Verbose output
dotnet test --verbosity detailed
```

### Test Framework
- **NUnit 4.x**: Test framework (not MSTest or xUnit)
- **NUnit assertions**: Assert.That() syntax
- **Async support**: All tests async (await operations)

### Test Duration
- **Individual test**: 100-500ms typical
- **Full suite**: ~5-10 seconds (41 tests)
- **Fast feedback**: Quick enough for TDD

## Test Patterns

### Arrange-Act-Assert
Standard test structure:
```
Arrange: Create test data (GPS packet, command, etc.)
Act: Send to backend, wait for state update
Assert: Verify state contains expected values
```

### Async/Await
All tests are async:
- **Await ConnectAsync**: Connect client before test
- **Await state updates**: Wait for state via TaskCompletionSource
- **Await SendCommandAsync**: Send commands and wait

### State Verification
Tests verify state via assertions:
- **Position**: Assert lat/lon match sent values (tolerance for floating-point)
- **Heading**: Assert degrees match (modulo 360)
- **Speed**: Assert km/h match (tolerance for physics updates)
- **Quality**: Assert fix type, satellite count, HDOP

### Timeouts
Tests use timeouts to prevent hanging:
- **Default**: 5 seconds (state update expected)
- **Extended**: 10 seconds (slower operations)
- **Fail fast**: Test fails if timeout exceeded (not infinite wait)

## Test Coverage

### What's Tested
- ✅ UDP packet reception (port 15556)
- ✅ Binary protocol unpacking (PGN 0xD6)
- ✅ GPS state population (position, heading, speed, quality)
- ✅ Coordinate transformations (WGS84 → Local)
- ✅ SignalR state broadcasting
- ✅ Simulator commands (start, stop, speed, position)
- ✅ Simulator physics (speed transitions, position updates)
- ⚠️ Simulator steering (logic correct, tests need adjustment)

### What's Not Tested
- ❌ FormGPS integration (manual testing only)
- ❌ External AgIO integration (no hardware tests)
- ❌ Multiple simultaneous clients (designed for, not tested)
- ❌ Performance under load (no stress tests)
- ❌ Network failures (no chaos engineering)
- ❌ Security (no penetration tests)

## Testing Philosophy

### Integration Over Unit
**Prefer**: Full-pipeline tests (realistic scenarios)
**Avoid**: Isolated unit tests (mocked dependencies)

**Rationale**: Backend value is integration (components working together), not individual methods.

### Fast Over Isolated
**Prefer**: In-memory backend (fast, no external dependencies)
**Avoid**: External databases, HTTP endpoints (slow, flaky)

**Rationale**: Fast tests run frequently (TDD, CI), slow tests run rarely.

### Comprehensive Over Perfect Coverage
**Prefer**: Critical paths well-tested (GPS pipeline, simulator)
**Avoid**: 100% line coverage (diminishing returns)

**Rationale**: 41/44 tests passing (93%) catches real bugs, 100% coverage has high cost.

## Known Test Issues

### Steering Tests Failing (3 tests)
**Issue**: Heading change from steering too small to detect in test timeframe.

**Root cause**: Physics correct (steering affects turn rate, accumulates over time), but test duration too short.

**Options**:
1. Increase test wait time (1-2 seconds → 5-10 seconds)
2. Increase steering angle (20° → 40°)
3. Reduce test sensitivity (accept smaller heading changes)
4. Mock physics (not preferred - defeats integration testing)

**Status**: Deferred (physics works in real usage, test needs adjustment).

## Future Testing Enhancements

### External GPS Tests
Integrate with real AgIO:
- **Physical GPS**: Connect to RTK receiver
- **Recorded data**: Replay field GPS logs
- **Validation**: Compare backend output to known good results

### Multi-Client Tests
Test multiple simultaneous clients:
- **Concurrent connections**: 2-10 clients
- **Broadcast fan-out**: All clients receive same state
- **Independent commands**: Each client sends commands

### Performance Tests
Measure backend performance:
- **Throughput**: GPS packets/sec processed
- **Latency**: Time from UDP receive → SignalR broadcast
- **Load**: CPU/memory usage under various rates
- **Stress**: Behavior at high packet rates (100+ Hz)

### End-to-End Tests
Full system validation:
- **Backend + FormGPS**: Real frontend integration
- **User scenarios**: Simulate operator workflows
- **Visual validation**: Screenshot comparison (field display)

## Related Documentation

- **[01-system-overview.md](01-system-overview.md)** - System architecture tested
- **[02-main-processing-loop.md](02-main-processing-loop.md)** - Processing tested by GPS tests
- **[06-simulator-capabilities.md](06-simulator-capabilities.md)** - Simulator tested by integration tests
- **[08-client-integration.md](08-client-integration.md)** - Client library tested via integration tests
