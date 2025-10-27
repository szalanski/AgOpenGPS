# Simulator Capabilities

## Built-In GPS Simulator

The backend includes a **built-in GPS simulator** for testing and development without physical GPS hardware. The simulator generates realistic vehicle movement and GPS data.

## Purpose

### Development
- **No GPS required**: Develop without expensive RTK GPS hardware
- **Repeatable**: Same conditions every test run
- **Controlled**: Set exact position, heading, speed
- **Fast iteration**: Instant feedback without field testing

### Testing
- **Integration tests**: Validate GPS processing pipeline
- **Edge cases**: Test GPS signal loss, poor quality, rapid movement
- **Performance**: Measure system latency and throughput
- **Regression**: Automated tests catch breaking changes

### Demonstration
- **Sales demos**: Show system without going to field
- **Training**: Teach operators without vehicle
- **Development previews**: Showcase new features

## Simulator Architecture

The simulator consists of three sub-systems:

### Vehicle Physics
Models realistic vehicle behavior:
- **Speed transitions**: Smooth acceleration and deceleration
- **Steering dynamics**: Smooth steering angle changes
- **Heading changes**: Vehicle turns based on steering and speed
- **Position updates**: Great circle navigation on Earth's surface

### GNSS Data Generation
Creates realistic GPS data:
- **Satellite simulation**: Realistic satellite count (10-14)
- **Fix quality**: RTK fix by default (best quality)
- **HDOP**: Quality metric based on satellite geometry
- **Age**: Correction data age (always fresh in simulator)
- **Altitude**: Terrain-based altitude variation

### Protocol Serialization
Encodes GPS data into binary format:
- **PGN 0xD6 format**: Same binary protocol as real GPS (AgIO)
- **128-byte packets**: Fixed-size packets
- **UDP transmission**: Sends to backend via UDP (localhost)

**Design benefit**: Three clear boundaries (Domain-Driven Design), each testable independently.

## Physics Model

### Speed Dynamics
Realistic vehicle acceleration and deceleration:

**Acceleration**: 0.86 km/h per physics tick
- At 93ms per tick: ~9.2 km/h per second acceleration
- Example: 0 → 10 km/h in ~1.1 seconds

**Deceleration**: 0.43 km/h per physics tick
- Half acceleration rate (realistic braking)
- Example: 10 km/h → 0 in ~2.2 seconds

**Smooth speed changes**: Not instant jumps (matches real vehicles)

### Steering Dynamics
Smooth steering transitions:
- **Target steering**: User sets desired angle
- **Smoothed steering**: Applied angle gradually approaches target
- **Smoothing factor**: Prevents instant steering changes
- **Realistic**: Matches hydraulic steering system lag

### Heading Changes
Vehicle turns based on steering and speed:
- **Turn rate**: Proportional to steering angle and speed
- **Great circle**: Follows Earth's curvature (accurate navigation)
- **Heading wrap**: Correctly handles 359° → 0° transition

### Position Updates
New position calculated each physics tick:
- **Step distance**: Speed × time (93ms)
- **Bearing**: Current heading
- **Great circle math**: Accurate position calculation (lat/lon)
- **Rate**: 93ms per tick (≈10.75 Hz)

## Simulator Control

### Start Command
Initializes simulator with initial conditions:
- **Position**: Latitude, longitude (WGS84)
- **Heading**: Direction (0-360°, 0=north)
- **Speed**: Initial speed (km/h)

Example: Start at (45.0°, -93.0°), heading north (0°), 10 km/h

### Stop Command
Pauses simulator:
- **GPS generation stops**: No more packets sent
- **State preserved**: Position, heading, speed remembered
- **Restart**: Start resumes from current state

### Speed Commands
Multiple ways to control speed:

**Speed Adjust**: Change speed by delta
- Add/subtract km/h from current speed
- Useful for increment/decrement buttons (repeat)
- Example: +1 km/h (speed up), -1 km/h (slow down)

**Speed Set (Instant)**: Jump to new speed immediately
- No acceleration curve (instant change)
- Useful for testing edge cases
- Example: 0 → 20 km/h instantly

**Speed Set (Smooth)**: Transition to new speed gradually
- Uses acceleration/deceleration curves
- Realistic vehicle behavior
- Example: 0 → 20 km/h over ~2 seconds

**Speed Zero**: Instant stop
- Sets speed to 0 immediately
- Emergency stop behavior

### Steering Commands
Control vehicle direction:

**Steering Set**: Set steering angle (degrees)
- Positive = right turn
- Negative = left turn
- Example: +20° (turn right)

**Steering Reset**: Center steering wheel
- Returns to 0° (straight ahead)
- Smooth transition (not instant)

### Direction Commands
Quick direction changes:

**Direction Reverse**: Flip heading 180°
- Instant reversal (vehicle turns around)
- Useful for testing opposite directions
- Example: Heading 45° → 225° (opposite)

### Position Commands
Teleport vehicle:

**Position Reset**: Return to starting position
- Teleports to initial coordinates (from Start command)
- Heading, speed unchanged (legacy behavior)
- Useful for restarting test runs

### Full Reset
Reset entire simulator:
- Returns to initial Start position
- Preserves speed and steering (legacy FormGPS compatibility)
- Equivalent to pressing "Reset Simulator" button

## Simulator Operation

### Timing
- **Physics tick**: 93ms intervals (fixed timer)
- **Packet rate**: ≈10.75 Hz (matches typical RTK GPS)
- **Independent**: Doesn't block main processing loop

### UDP Generation
Each physics tick:
1. **Update physics**: Compute new position, heading based on current state
2. **Generate GNSS data**: Create satellite count, HDOP, fix quality
3. **Serialize packet**: Encode into PGN 0xD6 binary format
4. **Send UDP**: Transmit to localhost:15556
5. **Backend receives**: Normal GPS processing pipeline

### Why 93ms?
- **Legacy compatibility**: Matches original FormGPS simulator timing
- **Realistic**: Close to 10 Hz (common RTK GPS rate)
- **Smooth**: Fast enough for smooth vehicle movement in display
- **Efficient**: Not too fast (unnecessary processing)

## Generated GPS Data

### Simulated Metrics
- **Latitude/Longitude**: Calculated from vehicle physics
- **Heading**: From steering and turning dynamics
- **Speed**: From speed control commands (smoothed)
- **Altitude**: Terrain-based (varies with position)
- **Satellites**: 10-14 (randomized, realistic range)
- **Fix quality**: RTK Fix (4) - best quality
- **HDOP**: 0.8 (excellent geometry)
- **Age**: 0.1 seconds (fresh corrections)

### Realism
- **Not random walk**: Physics-based movement (steering + speed)
- **Not GPS noise**: Perfect accuracy (simplifies testing)
- **Not signal loss**: Always full quality fix
- **Controlled**: Repeatable behavior for tests

## Comparison: Simulator vs Real GPS

| Aspect | Real GPS (AgIO) | Simulator |
|--------|-----------------|-----------|
| **Hardware** | Requires GPS receiver | No hardware needed |
| **Quality** | Variable (RTK, DGPS, GPS) | Always RTK Fix (perfect) |
| **Noise** | Position jitter (cm-level) | No noise (repeatable) |
| **Signal loss** | Can lose fix (buildings, trees) | Never loses signal |
| **Setup time** | Minutes (RTK convergence) | Instant (no convergence) |
| **Cost** | $1000+ (RTK GPS) | Free (built-in) |
| **Use case** | Production, real field work | Development, testing, demo |

## Integration with Backend

### Simulator is Part of Backend
- **Same process**: Runs inside AgOpenGPS.Api
- **Sends UDP**: Just like external GPS (AgIO)
- **Tests pipeline**: Full GPS processing path validated
- **No shortcuts**: Simulator doesn't bypass any processing

### Loopback Communication
```
Backend Process:
  ├─ Simulator (timer thread)
  │    ├─ Physics tick every 93ms
  │    ├─ Generate GPS packet
  │    └─ Send UDP to localhost:15556
  │
  └─ ApplicationOrchestrator (main loop)
       ├─ UDP Receiver (port 15556)
       ├─ Receive packet from simulator
       └─ Process (same as external GPS)
```

### Why Loopback UDP?
- **Tests UDP layer**: Validates network code
- **Realistic**: Mimics AgIO exactly (binary protocol, UDP)
- **Finds bugs**: Network issues caught in testing
- **Integration tests**: External GPS simulator can also connect

## Limitations

### Not a Full Vehicle Simulator
- **No engine**: No gear changes, torque curves
- **No suspension**: No bounce or roll from terrain
- **No implements**: No planter, sprayer, etc. simulation
- **No field**: No boundaries, obstacles, headlands

**Purpose**: GPS signal simulation only (not full vehicle dynamics)

### Not Real-World Conditions
- **No signal loss**: GPS never loses fix
- **No multipath**: No signal reflections (buildings, trees)
- **No ionosphere delay**: Perfect signal propagation
- **No satellite geometry**: HDOP doesn't vary realistically

**Purpose**: Controlled testing (not field condition validation)

### Not a Training Tool
- **No operator controls**: Direct command interface (not UI)
- **No realistic cabin**: No joystick, monitors, switches
- **No field planning**: No AB lines, headlands, boundaries

**Purpose**: Development and testing (not operator training)

## Future Enhancements

### Advanced Physics
- **Slip angle**: Tire slip based on speed and steering
- **Acceleration limits**: Max acceleration based on weight
- **Terrain slope**: Speed changes on hills
- **Wind effects**: Drift from crosswind

### Realistic GPS Degradation
- **Signal quality variation**: Random HDOP changes
- **Satellite masking**: Fix loss in simulated valleys
- **Multipath**: Position jitter near buildings
- **Convergence**: Gradual RTK fix acquisition

### Multi-Vehicle Simulation
- **Multiple tractors**: Simulate fleet operations
- **Different speeds**: Each vehicle independent
- **Collision detection**: Prevent overlap
- **Coordinated paths**: Leader-follower patterns

### Recorded Path Playback
- **Record real GPS**: Capture field data
- **Playback**: Repeat exact path in simulator
- **Analysis**: Compare algorithm performance
- **Regression testing**: Detect behavior changes

## Related Documentation

- **[10-simulator-domain.md](10-simulator-domain.md)** - Simulator internal design (DDD)
- **[07-command-handling.md](07-command-handling.md)** - Simulator control commands
- **[04-gnss-data-pipeline.md](04-gnss-data-pipeline.md)** - GPS processing
- **[05-network-communication.md](05-network-communication.md)** - UDP communication
- **[adr/005-simulator-domain-separation.md](adr/005-simulator-domain-separation.md)** - Why 3 sub-systems
- **[adr/007-udp-communication-pattern.md](adr/007-udp-communication-pattern.md)** - Why simulator uses UDP
