# ADR 006: Value Object Modeling

## Status
**Accepted**

## Context

GPS data represented as **primitive doubles**:
```csharp
double latitude = 45.0;
double longitude = -93.0;
double heading = 90.0;
double speed = 15.0;
```

**Problems with primitives**:
- **No type safety**: `latitude = heading;` compiles (nonsense assignment)
- **Invalid states possible**: `latitude = 999.0;` compiles (invalid coordinate)
- **Units unclear**: Is `heading` degrees or radians? Is `speed` km/h or m/s?
- **No validation**: Can create invalid GPS data (lat=999, lon=500, heading=-50)

## Decision

**Use value objects for domain primitives**. Wgs84Position, Heading, Speed, Altitude, SteeringAngle with invariant enforcement.

## Options Considered

### Option 1: Primitive Doubles
**Description**: Use `double` for all values (current legacy approach)

**Pros**:
- Simple (no extra classes)
- Familiar (everyone knows double)
- Fast (no object allocation)

**Cons**:
- No type safety (can assign latitude to heading)
- Invalid states possible (lat=999 compiles)
- Units unclear (degrees or radians?)
- No domain language (just numbers)

### Option 2: Wrapper Classes (Mutable)
**Description**: Classes that wrap primitives but allow modification

**Pros**:
- Type safety (can't assign Latitude to Heading)
- Validation possible (check in setters)

**Cons**:
- Mutable (value can change unexpectedly)
- Not thread-safe (shared instances can be modified)
- Validation scattered (setters, constructors, multiple places)

### Option 3: Value Objects (Immutable) ✅ **CHOSEN**
**Description**: Immutable classes/records with invariant enforcement

**Pros**:
- Type safety (compile-time checking)
- Invalid states impossible (invariants enforced at construction)
- Immutable (thread-safe, cacheable)
- Domain language (Position, Heading, Speed - not doubles)
- Self-documenting (units clear from type)

**Cons**:
- More types (more classes to understand)
- Slight overhead (object allocation)
- Verbose (need to construct objects, not just assign doubles)

## Rationale

**Type safety**: Compiler prevents nonsense assignments:
```csharp
Heading h = new Heading(90.0);
Speed s = h;  // Compiler error! Can't assign Heading to Speed
```

**Invalid states impossible**: Can't construct invalid value:
```csharp
new Wgs84Position(999.0, -93.0);  // Throws: Latitude out of range
new Heading(-50.0);  // Throws: Invalid heading
```

**Domain language**: Code matches how experts speak:
- "Vehicle heading is 90 degrees" → `Heading h = new Heading(90.0);`
- "Position is 45 north, 93 west" → `Wgs84Position p = new Wgs84Position(45.0, -93.0);`

**Immutability**: Thread-safe, predictable:
```csharp
Position p = new Wgs84Position(45.0, -93.0);
// p cannot be modified (immutable)
// Safe to share across threads
```

**Self-documenting**: Units clear from type:
- `Speed` is always km/h (not ambiguous double)
- `Heading` is always degrees 0-360 (not radians)
- `Altitude` is always meters (not feet)

## Consequences

### Positive
- ✅ **Type safety**: Compiler prevents invalid assignments (Heading ≠ Speed)
- ✅ **Invalid states impossible**: Can't create lat=999, heading=-50
- ✅ **Domain language**: Code matches expert vocabulary (Position, Heading, Speed)
- ✅ **Self-documenting**: Units clear from type (Speed is km/h, not ambiguous double)
- ✅ **Immutable**: Thread-safe, cacheable, predictable
- ✅ **Single validation point**: Invariants in constructor (not scattered)

### Negative
- ❌ **More types**: Need to understand Position, Heading, Speed, Altitude, SteeringAngle
- ❌ **Verbosity**: `new Heading(90.0)` vs `90.0` (more code)
- ❌ **Allocation overhead**: Objects allocate on heap (vs primitives on stack)

### Neutral
- ⚪ **C# 9.0 features**: Uses init-only setters, records (requires IsExternalInitPolyfill for .NET Standard 2.0)
- ⚪ **Value equality**: Value objects compare by value (not reference)

## Implementation

**Delivered in**: Workflow 002-003 (GPS/GNSS Migration + Simulator Refactoring)

**Value objects created**:

**Workflow 002 (GPS)**:
- `Wgs84Position`: Latitude (-90 to +90), Longitude (-180 to +180)
- `LocalPosition`: X, Y, Z meters (local coordinate frame)
- `Heading`: Degrees (0-360, wraps)
- `Speed`: Kilometers per hour
- `Altitude`: Meters above sea level

**Workflow 003 (Simulator)**:
- `SteeringAngle`: Degrees (typical range -40 to +40, vehicle-dependent)

**Implementation patterns**:
- **Immutable**: C# records with init-only setters
- **Validation**: Constructor throws if invalid
- **Value equality**: Records provide value-based equality
- **Conversion methods**: `Heading.ToRadians()`, `Speed.MetersPerSecond`

**Example**:
```csharp
public record Wgs84Position
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public Wgs84Position(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }
}
```

**Commit reference**: See Workflow 002 task 2 (GnssState model) and Workflow 003 task 1 (SteeringAngle)

**Date**: Workflow 002-003 completion (2025)

## Related Decisions

- **[005-simulator-domain-separation.md](005-simulator-domain-separation.md)**: Domain services use value objects
- **[009-domain-model.md](../09-domain-model.md)**: Full value object descriptions

## System Documentation

- **[../09-domain-model.md](../09-domain-model.md)**: Core domain concepts (value objects)
- **[../04-gnss-data-pipeline.md](../04-gnss-data-pipeline.md)**: GPS pipeline uses value objects

## References

- **Workflow 002 Plan**: [../../workflow/002-gps-gnss-migration/plan.md](../../../workflow/002-gps-gnss-migration/plan.md)
- **Workflow 003 Plan**: [../../workflow/003-simulator-domain-model/plan.md](../../../workflow/003-simulator-domain-model/plan.md)
- **Value Object pattern**: https://martinfowler.com/bliki/ValueObject.html
- **Domain-Driven Design**: Eric Evans, "Domain-Driven Design: Tackling Complexity in the Heart of Software"
