# Simulator Domain

## Domain-Driven Design Refactoring

The simulator was refactored from a single monolithic service into **three bounded contexts** following Domain-Driven Design (DDD) principles.

## Bounded Contexts

### What is a Bounded Context?
A **bounded context** is a clear boundary within which a domain model is defined and applicable. Each context has:
- **Clear purpose**: Specific responsibility
- **Own language**: Terms have specific meaning within context
- **Internal model**: Domain concepts and rules
- **Explicit interfaces**: How it collaborates with other contexts

**Benefit**: Large systems decompose into manageable, independently understandable pieces.

## Simulator Architecture

### Three Bounded Contexts

**1. Vehicle Physics (Domain)**
- **Responsibility**: Vehicle motion and dynamics
- **Concepts**: Speed transitions, steering dynamics, turning, position calculations
- **Purpose**: Realistic vehicle movement simulation

**2. GNSS Data Generation (Domain)**
- **Responsibility**: GPS signal characteristics
- **Concepts**: Satellites, fix quality, HDOP, altitude, correction age
- **Purpose**: Realistic GPS receiver output simulation

**3. AgIO Protocol Serialization (Infrastructure)**
- **Responsibility**: Binary protocol encoding
- **Concepts**: Packet format, byte layout, checksum
- **Purpose**: Convert domain data to wire format

### Why Three Contexts?

#### Vehicle Physics
**Domain**: Vehicle kinematics and dynamics

**Knowledge required**: Agriculture vehicle behavior, steering systems, great circle navigation.

**Not GPS-specific**: Any vehicle simulation needs this (car, tractor, drone).

**Examples**:
- "Vehicle accelerates smoothly, not instant jumps"
- "Steering angle changes gradually (hydraulic lag)"
- "Turn rate depends on speed and steering angle"
- "Position updates follow great circle (Earth's curvature)"

#### GNSS Data Generation
**Domain**: GPS receiver characteristics

**Knowledge required**: GPS signal quality, satellite constellation behavior, error sources.

**Not vehicle-specific**: Any GPS simulation needs this (stationary receiver, vehicle, aircraft).

**Examples**:
- "Satellite count varies 10-14 (realistic range)"
- "RTK fix is best quality (HDOP 0.8)"
- "Altitude varies with terrain (not flat everywhere)"
- "Correction age stays low (fresh RTK data)"

#### AgIO Protocol Serialization
**Infrastructure**: Binary encoding

**Knowledge required**: AgOpenGPS wire protocol, byte ordering, packet structure.

**Not domain logic**: Pure technical concern (data formatting).

**Examples**:
- "Heading stored as unsigned short * 100"
- "Packet is 128 bytes fixed size"
- "PGN 0xD6 header identifies GPS packet"
- "Latitude/Longitude stored as doubles"

### Context Independence

Each context can be:
- **Understood alone**: No need to understand others
- **Tested alone**: Unit tests for single context
- **Replaced**: Swap implementations without affecting others
- **Reused**: Use in different systems

## Vehicle Physics Service

### Responsibilities

**Speed Transitions**:
- **Acceleration**: Smooth speed increase (0.86 km/h per tick)
- **Deceleration**: Smooth speed decrease (0.43 km/h per tick)
- **Clamping**: Keep speed within valid range (-21 to 322 km/h)
- **Target tracking**: Gradually approach target speed

**Steering Dynamics**:
- **Smoothing**: Steering wheel doesn't jump instantly
- **Lag modeling**: Hydraulic steering system delay
- **Angle limits**: Practical steering range

**Heading Changes**:
- **Turn rate**: Calculate from steering angle and speed
- **Great circle**: Account for Earth's curvature
- **Heading wrap**: 359° + 2° = 1° (not 361°)

**Position Updates**:
- **Step distance**: Distance traveled in one tick (speed × time)
- **Bearing**: Current heading
- **New position**: Calculate lat/lon after moving distance at bearing
- **Accuracy**: Great circle navigation (not flat Earth approximation)

### Domain Logic Examples

**Speed transition logic**:
```
If current speed < target speed:
  Accelerate at 0.86 km/h per tick
Else if current speed > target speed:
  Decelerate at 0.43 km/h per tick
Else:
  Speed matches target (no change)
```

**Steering smoothing logic**:
```
Difference = target angle - current angle
Smoothed change = difference × smoothing factor (not instant)
New angle = current angle + smoothed change
```

**Heading change logic**:
```
Turn rate = f(steering angle, wheelbase, speed)
Heading change = turn rate × time step
New heading = normalize(old heading + heading change)
```

### No Infrastructure
Vehicle Physics doesn't know about:
- UDP packets (doesn't send/receive network data)
- Threads or timers (doesn't manage execution)
- SignalR (doesn't broadcast)
- Files or databases (no persistence)

**Pure domain logic**: Physics calculations only.

## GNSS Data Generator

### Responsibilities

**Satellite Simulation**:
- **Count**: Random 10-14 satellites (realistic range)
- **Variation**: Changes over time (satellites rise/set)
- **No orbital mechanics**: Simplified (don't calculate satellite positions)

**Fix Quality**:
- **Always RTK Fix**: Simulator provides best quality
- **No degradation**: Signal never lost (testing convenience)
- **Future**: Could add quality variation (loss of fix, DGPS fallback)

**HDOP (Horizontal Dilution of Precision)**:
- **Static value**: 0.8 (excellent geometry)
- **Future**: Could vary with satellite count and positions

**Altitude Simulation**:
- **Terrain-based**: Varies with position (not flat)
- **Realistic**: Gentle slopes (not random noise)
- **Smooth**: No sudden jumps

**Correction Age**:
- **Fresh data**: Always 0.1 seconds (recent corrections)
- **RTK characteristic**: Age matters for RTK quality

### Domain Logic Examples

**Satellite count variation**:
```
Base count: 12 satellites
Random variation: ±2 satellites
Result: 10-14 satellites (realistic range)
Change rate: Slow (satellites don't vanish instantly)
```

**Altitude calculation**:
```
Terrain function: f(latitude, longitude)
Not flat: Gentle hills (sine wave or noise-based)
Smooth: No discontinuities (vehicle doesn't teleport vertically)
```

### No Vehicle Coupling
GNSS Generator doesn't know about:
- Vehicle speed or heading (satellites don't care)
- Steering (GPS sees position only, not steering wheel)
- Physics (independent of vehicle dynamics)

**Decoupled**: Could generate GPS data for stationary receiver.

## AgIO Protocol Serializer

### Responsibilities

**Binary Encoding**:
- **PGN 0xD6 format**: 128-byte GPS packet
- **Field layout**: Latitude, longitude, heading, speed, altitude, quality at fixed offsets
- **Data types**: Doubles, shorts, bytes (platform-independent)
- **Unit scaling**: Convert real units to wire format (e.g., heading × 100)

**Packet Structure**:
```
Byte 0-7:   Latitude (double, degrees)
Byte 8-15:  Longitude (double, degrees)
Byte 16-17: Heading (ushort, degrees × 100)
Byte 18-19: Speed (ushort, km/h × 100)
Byte 20-23: Altitude (float, meters)
Byte 24:    Fix quality (byte, enum value)
Byte 25:    Satellite count (byte)
Byte 26-27: HDOP (ushort, value × 100)
Byte 28-29: Age (ushort, seconds × 100)
Byte 30-127: Reserved (future extensions)
```

**Checksum** (if required):
- Data integrity verification
- Detect corruption in transit

### Infrastructure Concern
Protocol serialization is **not domain logic**:
- **Technical**: Byte manipulation, encoding rules
- **No business rules**: Doesn't know what data means
- **Protocol spec**: Follows external AgOpenGPS standard
- **Replaceable**: Could swap for JSON, Protobuf, etc.

### No Domain Coupling
Serializer doesn't know about:
- Physics (doesn't calculate position)
- GPS quality (doesn't determine fix type)
- Simulator (doesn't control timing)

**Pure transformation**: Domain objects → Bytes

## Service Collaboration

### Simulator Service Orchestration
**SimulatorService** orchestrates the three services:

```
Simulator tick (every 93ms):
  ├─ VehiclePhysicsService
  │    ├─ Update speed (acceleration/deceleration)
  │    ├─ Smooth steering
  │    ├─ Calculate heading change
  │    └─ Compute new position
  │
  ├─ GnssDataGenerator
  │    ├─ Get satellite count
  │    ├─ Get fix quality (always RTK)
  │    ├─ Get HDOP (0.8)
  │    ├─ Calculate altitude (terrain-based)
  │    └─ Get correction age (0.1s)
  │
  └─ AgIoProtocolSerializer
       ├─ Encode position (lat/lon)
       ├─ Encode heading, speed, altitude
       ├─ Encode quality metrics
       └─ Return 128-byte packet
```

**Simulator knows all three contexts** (orchestration layer), but each service **doesn't know about others** (loose coupling).

## Benefits of Separation

### Clarity
Each service has single, clear responsibility:
- **Physics**: How vehicle moves
- **GNSS**: What GPS reports
- **Serializer**: How data is encoded

**No confusion**: Developer knows exactly where logic belongs.

### Testability
Each service can be tested independently:
- **Physics tests**: Give speed/steering, assert new position
- **GNSS tests**: Assert satellite count in range, HDOP reasonable
- **Serializer tests**: Encode/decode, assert round-trip correctness

**No mocking**: Services don't depend on each other.

### Reusability
Services can be used elsewhere:
- **Physics**: Reuse in guidance simulator (predict vehicle path)
- **GNSS**: Reuse in GPS receiver simulator (test GPS processing)
- **Serializer**: Reuse in AgIO client (encode/decode packets)

### Maintainability
Changes localize:
- **Physics change**: Improve turn rate calculation (doesn't affect GNSS or protocol)
- **GNSS change**: Add satellite masking (doesn't affect physics or protocol)
- **Protocol change**: Add IMU data (doesn't affect physics or GNSS logic)

## DDD Patterns Applied

### Value Objects
All parameters are value objects:
- **Position**: Wgs84Position (not two doubles)
- **Heading**: Heading (not raw double)
- **Speed**: Speed (not primitive)
- **Steering**: SteeringAngle (typed)

**Benefit**: Type safety, domain language.

### Domain Services
Logic that doesn't belong to single entity:
- **VehiclePhysicsService**: Operations on vehicle state
- **GnssDataGenerator**: GPS signal characteristics
- Not entities (no identity), not value objects (contain logic)

### Bounded Contexts
Clear boundaries between:
- **Vehicle domain**: Kinematics and dynamics
- **GPS domain**: Signal characteristics
- **Infrastructure**: Protocol encoding

### Ubiquitous Language
Terms match domain expert vocabulary:
- "Turn rate" (not "angular velocity")
- "Great circle navigation" (not "geodetic calculation")
- "Satellite count" (not "visible satellites in constellation")

## What This Is NOT

### Not Layered Architecture
Layers (presentation, business, data) cut horizontally:
```
Presentation Layer (UI)
Business Layer (Logic)
Data Layer (Storage)
```

Bounded contexts cut vertically:
```
Physics Context | GNSS Context | Protocol Context
(all layers)    | (all layers) | (all layers)
```

### Not Microservices
Services are in-process (not separate processes):
- **Same process**: All services in AgOpenGPS.Api
- **Method calls**: Not HTTP or gRPC
- **No deployment isolation**: Single deployment unit

**Future**: Could extract to microservices if needed.

### Not Event Sourcing
Events not persisted:
- **No event store**: Simulator state not saved
- **No replay**: Can't recreate history
- **Stateful**: Current state only (not event log)

## Future Bounded Contexts

As system grows, more contexts will emerge:

**Guidance Context**:
- AB line calculations
- Curve generation
- Cross-track error

**Section Control Context**:
- Section on/off logic
- Coverage mapping
- Implement geometry

**AutoSteer Context**:
- PID controller
- Steering command generation
- Stability management

Each context will have clear boundaries and responsibilities.

## Related Documentation

- **[06-simulator-capabilities.md](06-simulator-capabilities.md)** - What simulator does (external view)
- **[09-domain-model.md](09-domain-model.md)** - Value objects used by simulator
- **[07-command-handling.md](07-command-handling.md)** - How simulator is controlled
- **[adr/005-simulator-domain-separation.md](adr/005-simulator-domain-separation.md)** - Why three contexts
