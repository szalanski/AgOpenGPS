# Domain Model

## Core Domain Concepts

The system models precision agriculture concepts using **value objects** that represent domain primitives with type safety and invariant protection.

## Value Objects vs Primitives

### Traditional Approach (Primitives)
```
double latitude = 999.0;  // Invalid! But compiles
double heading = -50.0;   // Invalid! But compiles
double speed = latitude;  // Nonsense! But compiles
```

**Problems**: No type safety, invalid states possible, units unclear.

### Value Object Approach (Current System)
```
Wgs84Position position = new Wgs84Position(999.0, -93.0);  // Throws exception
Heading heading = new Heading(-50.0);                      // Throws exception
Speed speed = heading;                                      // Compiler error
```

**Benefits**: Invariants enforced, type safety, impossible to create invalid state.

## Geographic Coordinates

### WGS84 Position (Global Coordinates)
Represents a point on Earth's surface in GPS coordinates:

**Properties**:
- **Latitude**: -90° to +90° (negative = south, positive = north)
- **Longitude**: -180° to +180° (negative = west, positive = east)

**Validation**:
- Constructor throws if latitude out of range
- Constructor throws if longitude out of range
- Once created, guaranteed valid

**Use Cases**:
- GPS receiver output (native format)
- Waypoints and field boundaries
- Global position storage
- Coordinate system transformations

### Local Position (Field Coordinates)
Represents a point in local Cartesian plane (field reference frame):

**Properties**:
- **X**: East-West axis (meters, positive = east)
- **Y**: North-South axis (meters, positive = north)
- **Z**: Vertical axis (meters, positive = up)

**Origin**:
- User-defined field reference point
- Typically field entrance or boundary corner
- Set once at field start (via InitializeLocalPlane)

**Use Cases**:
- Field mapping and visualization
- Guidance calculations (straight lines in meters)
- Implement control (section on/off based on position)
- Distance and area calculations

### Coordinate Transformation
System maintains both coordinate systems:
- **Input**: GPS provides WGS84
- **Transform**: Backend converts WGS84 → Local
- **Output**: State contains both (clients choose)

**Why Both**:
- **WGS84**: Universal, portable, works anywhere
- **Local**: Simple geometry, intuitive for operators

## Motion and Orientation

### Heading
Vehicle direction (compass heading):

**Range**: 0° to 360° (modulo arithmetic)
- **0°**: North
- **90°**: East
- **180°**: South
- **270°**: West

**Properties**:
- **Degrees**: Public property (0-360)
- **ToRadians()**: Convert to radians for calculations
- **Normalization**: Automatically handles wrap (359° + 2° = 1°)

**Use Cases**:
- Vehicle direction display
- Turning calculations
- Guidance line alignment
- Map rotation

### Speed
Vehicle ground speed:

**Unit**: Kilometers per hour (km/h)
**Range**: -21 to 322 km/h (supports reverse, practical upper limit)

**Properties**:
- **KilometersPerHour**: Public property
- **MetersPerSecond**: Conversion method (for physics)

**Use Cases**:
- Speed display
- Motion calculations (distance per time)
- Implement rate control (application varies with speed)
- Safety limits (warnings, automation cutoffs)

### Altitude
Elevation above sea level:

**Unit**: Meters
**Reference**: WGS84 ellipsoid (not mean sea level)

**Properties**:
- **Meters**: Public property
- **Simulated**: Terrain-based in simulator (realistic variation)

**Use Cases**:
- Topographic field mapping
- Drainage planning
- Slope calculations (future)
- Elevation-aware guidance (future)

### Steering Angle
Steering wheel position:

**Unit**: Degrees
**Range**: Typically -40° to +40° (depends on vehicle)
- **Positive**: Right turn
- **Negative**: Left turn
- **Zero**: Straight ahead

**Properties**:
- **Degrees**: Public property
- **Zero**: Static constant for center position
- **Validation**: Practical limits enforced

**Use Cases**:
- Simulator control (vehicle turning)
- AutoSteer feedback (where is steering wheel)
- Steering dynamics (smooth transitions)

## GPS Quality Metrics

### GPS Quality (Fix Type)
Enum representing GPS fix quality:

**Values**:
- **NoFix (0)**: GPS not working, no position
- **GpsFix (1)**: Standard GPS (meter-level, ~3-10m accuracy)
- **DGpsFix (2)**: Differential GPS (decimeter, ~0.5-1m accuracy)
- **RtkFloat (5)**: RTK float solution (cm-level, ~5-20cm)
- **RtkFix (4)**: RTK fixed solution (cm-level, ~1-3cm, best)

**Use Cases**:
- Quality indicator display (icon, color)
- Guidance enable/disable (require RTK for auto-steer)
- Data filtering (ignore low-quality fixes)

### GPS Health
Enum representing overall GPS health status:

**Values**:
- **Invalid**: No valid GPS data
- **Poor**: GPS fix but poor geometry or quality
- **Good**: Acceptable GPS for manual guidance
- **Excellent**: RTK fix, optimal for auto-steer

**Computed From**:
- Fix quality (RTK > DGPS > GPS)
- Satellite count (more is better)
- HDOP (geometric quality)
- Age (correction data freshness)

**Use Cases**:
- Simple health indicator for operator
- Automation decisions (enable/disable features)
- Warnings (GPS degrading, losing fix)

## Application State

### ApplicationState
Top-level state broadcast from backend:

**Properties**:
- **Timestamp**: When backend processed this state (UTC)
- **Gnss**: GPS/GNSS subsystem state (GnssState)

**Future Properties** (as more subsystems migrate):
- **Guidance**: Guidance subsystem state (AB line, curves)
- **Sections**: Section control state (on/off, coverage)
- **Vehicle**: Vehicle state (position corrections, roll/pitch)
- **Boundaries**: Field boundary and headland state
- **AutoSteer**: Auto-steering state (engaged, PID values)

### GnssState
Complete GPS subsystem state:

**Properties**:
- **Position**: Wgs84Position (global coordinates)
- **LocalPosition**: LocalPosition (field coordinates)
- **Heading**: Heading (degrees)
- **Speed**: Speed (km/h)
- **Altitude**: Altitude (meters)
- **Quality**: GpsQuality (fix type)
- **Health**: GpsHealth (overall status)
- **SatelliteCount**: Number of satellites
- **Hdop**: Horizontal dilution of precision
- **Age**: Correction data age (seconds)
- **FixTime**: GPS timestamp (future)

**Design**: Complete GPS state snapshot (everything UI needs to display).

## Domain Language

### Ubiquitous Language
Domain model uses agriculture and GPS terminology (not programming jargon):

**Good (Domain)**:
- **Heading** (not "Angle" or "Direction")
- **WGS84** (not "GlobalCoordinates")
- **RTK Fix** (not "HighAccuracyMode")
- **HDOP** (not "GeometricQualityMetric")

**Bad (Technical)**:
- "Vector2D" (use Position or Heading)
- "Timestamp" (use "FixTime" or "ReceivedAt")
- "QualityLevel" (use GPS terminology: GpsFix, RtkFix)

**Benefit**: Code matches how GPS experts, agronomists, and operators speak.

## Value Object Characteristics

### Immutability
Value objects are immutable (cannot change after creation):

**Construction**:
```
Position p = new Wgs84Position(45.0, -93.0);
// p is now fixed, cannot modify latitude or longitude
```

**Modification** (creates new instance):
```
Position p2 = new Wgs84Position(45.1, -93.0);  // New object
// Original p unchanged
```

**Benefits**:
- Thread-safe (safe to share across threads)
- Predictable (value doesn't change unexpectedly)
- Cacheable (can cache safely)

### Equality
Value objects compare by value (not reference):

```
Position p1 = new Wgs84Position(45.0, -93.0);
Position p2 = new Wgs84Position(45.0, -93.0);
// p1 == p2  (true! Same coordinates)
```

**Benefits**:
- Natural comparison semantics
- Works in dictionaries and hash sets
- Matches domain expectations

### Self-Validation
Value objects enforce invariants at construction:

```
new Heading(400.0);  // Throws: Invalid heading
new Speed(1000.0);   // Throws: Speed out of range
```

**Benefits**:
- Invalid states impossible (can't exist)
- No scattered validation checks
- Single point of truth (invariants in constructor)

## C# 9.0 Features

### Init-Only Setters
Value objects use init-only properties:

```
record Wgs84Position
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
```

**Benefits**:
- Immutable after construction
- Constructor or object initializer syntax
- .NET Standard 2.0 compatible (IsExternalInitPolyfill)

### Record Types
Some value objects are C# records:

```
public record Wgs84Position(double Latitude, double Longitude);
```

**Benefits**:
- Concise syntax (single line)
- Value equality built-in
- ToString() generated
- Immutable by default

**Note**: Not all value objects are records (some need custom validation logic).

## Domain Model Evolution

### Current Scope
GPS/GNSS subsystem only:
- Position, heading, speed (vehicle motion)
- GPS quality metrics (fix, satellites, HDOP)
- Coordinate systems (WGS84, Local)

### Future Expansion
As more subsystems migrate:

**Guidance Domain**:
- AB Line, Curve, Contour (guidance paths)
- Offset, Snap distance (guidance parameters)
- Cross-track error (distance from line)

**Section Control Domain**:
- Section (on/off state)
- Implement (tool configuration)
- Coverage map (field coverage)

**Vehicle Domain**:
- Wheelbase, antenna offset (vehicle geometry)
- Roll, pitch (IMU orientation)
- Pivot correction (antenna position adjustment)

**Field Domain**:
- Boundary (field perimeter)
- Headland (turn area)
- Obstacle (trees, buildings)

## Related Documentation

- **[04-gnss-data-pipeline.md](04-gnss-data-pipeline.md)** - How GPS data populates domain models
- **[06-simulator-capabilities.md](06-simulator-capabilities.md)** - Simulated domain objects
- **[08-client-integration.md](08-client-integration.md)** - How clients receive domain state
- **[10-simulator-domain.md](10-simulator-domain.md)** - Simulator domain design
- **[adr/006-value-object-modeling.md](adr/006-value-object-modeling.md)** - Why value objects
