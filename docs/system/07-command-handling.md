# Command Handling

## Command/Query Separation (CQRS)

The system separates **commands** (change state) from **queries** (read state) using the CQRS pattern.

### Queries (Backend → Client)
- **State updates**: Backend pushes application state to clients
- **Read-only**: Clients receive data but don't request it
- **Push model**: No GET requests or query methods
- **Rate**: GPS-driven (5-10 Hz variable)

### Commands (Client → Backend)
- **User actions**: Client sends commands when user acts
- **Write-only**: Commands change state, don't return data
- **On-demand**: Only when user interacts
- **Rate**: Sporadic (typically <1/sec)

**Benefit**: Clear separation prevents query/command conflation (RESTful CRUD confusion).

## Command Pattern Implementation

### Command Structure
Commands are **strongly-typed objects** (not strings or dictionaries):

```
Command Interface (marker)
  ├─ Command Type (which command)
  ├─ Command Parameters (typed properties)
  └─ Validation Rules (invariants enforced)
```

### Type Safety
- **Compile-time checking**: Wrong types caught by compiler
- **IDE support**: IntelliSense shows available commands
- **Refactoring**: Renames propagate automatically
- **No magic strings**: Commands are typed objects

### Command Routing
```
Client creates command
  ├─ SignalR Hub receives command
  ├─ Hub dispatches to MediatR
  ├─ MediatR routes to handler (by command type)
  ├─ Handler processes command
  └─ State changes propagate via normal state updates
```

**Pattern**: Hub is thin router (no business logic), handlers contain logic.

## MediatR Integration

### What is MediatR?
- **Mediator pattern**: Decouples senders from receivers
- **Command dispatcher**: Routes commands to handlers
- **Open/Closed principle**: Add new commands without modifying hub

### Handler Registration
Handlers registered automatically at startup:
- **Assembly scanning**: MediatR finds all handlers
- **Dependency injection**: Handlers receive dependencies
- **Lifetime**: Transient (new instance per command)

### Handler Pattern
```
Handler
  ├─ Receives command
  ├─ Validates parameters (optional)
  ├─ Calls domain service (business logic)
  └─ Returns (no result, void)
```

**Commands don't return values** - state updates provide feedback.

## Simulator Commands

Currently implemented commands (simulator control):

### Start
- **Purpose**: Begin simulator with initial conditions
- **Parameters**: Position (lat/lon), heading (degrees), speed (km/h)
- **Effect**: Simulator starts generating GPS packets
- **Example**: Start at (45.0, -93.0), heading 90°, 15 km/h

### Stop
- **Purpose**: Pause simulator
- **Parameters**: None
- **Effect**: GPS packet generation stops, state preserved

### Speed Commands

**Speed Adjust**
- **Purpose**: Change speed by delta (increment/decrement)
- **Parameters**: Delta (km/h, positive or negative)
- **Effect**: Target speed += delta, smooth transition
- **Use case**: Repeat buttons (hold to accelerate)

**Speed Set (Instant)**
- **Purpose**: Jump to speed immediately
- **Parameters**: Speed (km/h)
- **Effect**: Current speed = target speed (no transition)

**Speed Set (Smooth)**
- **Purpose**: Transition to speed gradually
- **Parameters**: Speed (km/h)
- **Effect**: Target speed set, acceleration/deceleration applied

**Speed Zero**
- **Purpose**: Emergency stop
- **Parameters**: None
- **Effect**: Speed set to 0 instantly

### Steering Commands

**Steering Set**
- **Purpose**: Set steering wheel angle
- **Parameters**: Angle (degrees, + = right, - = left)
- **Effect**: Target steering set, smoothing applied

**Steering Reset**
- **Purpose**: Center steering wheel
- **Parameters**: None
- **Effect**: Target steering = 0° (straight)

### Direction Commands

**Direction Reverse**
- **Purpose**: Turn vehicle around (180° instant)
- **Parameters**: None
- **Effect**: Heading += 180° (wraps at 360°)

**Position Reset**
- **Purpose**: Teleport to start position
- **Parameters**: None
- **Effect**: Position resets, heading/speed unchanged

**Full Reset**
- **Purpose**: Reset entire simulator
- **Parameters**: None
- **Effect**: Position resets (legacy FormGPS behavior)

## Command Event Pattern

### Unified Command
Single command type with event payload:
- **Command**: `UpdateSimulatorCommand`
- **Event**: `SimulatorEvent` (11 event types)
- **Parameters**: Event-specific typed properties

### Event Types
Events represent domain actions (not technical operations):
- **Start** (not "InitializeSimulator")
- **Stop** (not "PauseSimulator")
- **SpeedAdjust** (not "ChangeSpeedByDelta")
- **SpeedSet** (not "SetSpeedValue")
- etc.

**Domain language**: Events match how operators think and speak.

### Factory Methods
Type-safe event creation:
```
SimulatorEvent.Start(position, heading, speed)
SimulatorEvent.SpeedAdjust(1.0)  // +1 km/h
SimulatorEvent.SteeringSet(angle)
```

**Benefits**: Discoverable API, compile-time safety, IDE autocomplete.

## SignalR Hub Methods

### Hub Limitation
SignalR doesn't support generic hub methods:
- **Not supported**: `SendCommand<T>(T command)`
- **Workaround**: Specific method per command type

### Current Implementation
Unified method with event payload:
```
Hub method: UpdateSimulator(UpdateSimulatorCommand command)
  ├─ Command contains: SimulatorEvent
  ├─ Event contains: Type + parameters
  └─ Handler routes by event type
```

**Trade-off**: Single hub method (clean), routing in handler (one place).

## Command Processing Flow

### End-to-End Example: Speed Up
```
1. User clicks "Speed Up" button (FormGPS)
2. FormGPS creates: SimulatorEvent.SpeedAdjust(+1.0)
3. FormGPS wraps: UpdateSimulatorCommand(event)
4. FormGPS sends: backend.SendCommandAsync(command)
5. SignalR Hub receives: UpdateSimulator(command)
6. Hub dispatches: mediator.Send(command)
7. MediatR routes: UpdateSimulatorCommandHandler
8. Handler calls: simulator.ProcessEvent(event)
9. Simulator adjusts: targetSpeed += 1.0 km/h
10. Next physics tick: Speed increases gradually
11. GPS packet generated: Contains new speed
12. ApplicationOrchestrator processes: Broadcasts state
13. FormGPS receives: State update with new speed
14. UI updates: Speed display shows new value
```

**Latency**: ~15-20ms (command → visible UI change)

## Command Validation

### Client-Side Validation
FormGPS validates before sending:
- **Range checks**: Speed within [-21, 322] km/h
- **Type checks**: Steering angle is number
- **State checks**: Can't start if already running

**Purpose**: Immediate feedback (no network round-trip)

### Backend Validation
Handler validates on receive:
- **Range clamping**: Speed clamped to valid range
- **Null checks**: Required parameters present
- **State validation**: Commands valid for current state

**Purpose**: Defense in depth (never trust client)

## Error Handling

### Invalid Commands
If command invalid:
- **Backend logs error**: Record validation failure
- **Command dropped**: No state change
- **Client unaware**: No error returned (fire-and-forget)
- **State unchanged**: Next state update shows no change

**User sees**: Nothing happens (button press ignored)

### Handler Exceptions
If handler throws:
- **Backend logs exception**: Stack trace recorded
- **Command dropped**: Partial changes rolled back (if possible)
- **System continues**: Other commands unaffected

**Resilience**: Single command failure doesn't crash backend.

## Future Commands (Not Yet Implemented)

### Guidance Commands
- **SetABLine**: Define guidance line
- **EnableGuidance**: Activate auto-guidance
- **OffsetLine**: Shift guidance parallel

### Section Control Commands
- **EnableSection**: Turn on/off section
- **SetSectionWidth**: Configure implement width
- **ManualOverride**: Force section on/off

### Field Commands
- **LoadField**: Load saved field boundary
- **SaveField**: Persist current field
- **CreateBoundary**: Define field edge

### AutoSteer Commands
- **SetSteerMode**: Manual/GPS/AutoSteer
- **CalibrateSteering**: Run steering calibration
- **AdjustGain**: Tune PID controller

## Command vs Event Sourcing

### This System: Commands (No Event Store)
- **Commands**: Actions to perform NOW
- **No history**: Commands not persisted
- **Current state**: Only latest state stored
- **Simple**: Minimal complexity

### Event Sourcing (Not Implemented)
- **Events**: What happened in PAST
- **Event store**: All events persisted
- **Replay**: Rebuild state from events
- **Complex**: More infrastructure needed

**Trade-off**: Simpler implementation vs audit trail capability.

## Testing Commands

### Integration Tests
Tests send actual commands:
```
1. Create command
2. Send via SignalR (in-memory)
3. Wait for state update
4. Assert state changed correctly
```

**Tests full pipeline**: Hub → Handler → Domain → State.

### Unit Tests
Not used - integration tests sufficient:
- **Commands simple**: Just routing (minimal logic)
- **Integration tests**: Cover full path (more valuable)
- **Fast enough**: Integration tests run quickly (in-memory)

## Related Documentation

- **[03-real-time-communication.md](03-real-time-communication.md)** - Bidirectional communication
- **[06-simulator-capabilities.md](06-simulator-capabilities.md)** - Simulator control
- **[08-client-integration.md](08-client-integration.md)** - How clients send commands
- **[10-simulator-domain.md](10-simulator-domain.md)** - Command processing internals
- **[adr/004-command-query-separation.md](adr/004-command-query-separation.md)** - Why CQRS pattern
