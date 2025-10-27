# Simulator Comparison: Legacy (CSim) vs API (SimulatorService)

## Overview

This document compares the legacy WinForms simulator ([CSim.cs](../SourceCode/GPS/Classes/CSim.cs)) with the new backend API simulator ([SimulatorService.cs](../SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs)).

Both simulators share the same core physics algorithms but differ significantly in their control architecture and output mechanisms.

---

## Side-by-Side Comparison Table

| Aspect | Legacy (CSim) | New (SimulatorService) |
|--------|---------------|------------------------|
| **Location** | `SourceCode/GPS/Classes/CSim.cs` | `SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs` |
| **Timer Interval** | 93ms (10.75 Hz) | 93ms (10.75 Hz) ✓ **Same** |
| **Timer Implementation** | WinForms Timer (`timerSim`) | HostedService (`SimulatorHostedService`) |
| **Primary Control** | `stepDistance` (distance per tick) | `speedKmh` (speed in km/h) |
| **Speed Calculation** | `speed = \|4 × stepDistance × 10\|` | `stepDistance = (speedKmh / 3600) × 0.093` |
| **Steering Smoothing** | Non-linear damping (±6°/±2°/±0.5°) | ✓ **Same algorithm** |
| **Heading Update** | `Δheading = stepDistance × tan(θ) / 2` | ✓ **Same formula** |
| **Position Update** | `Wgs84.CalculateNewPosition...()` | Great circle navigation (explicit) |
| **Altitude Simulation** | Pseudo-random from lat/lon | ✓ **Same algorithm** |
| **GPS Quality** | HDOP=0.7, Satellites=12 | ✓ **Same values** |
| **Fix Quality** | Not set in CSim | RTK Fixed (4) |
| **Output Mechanism** | Writes to `CNMEA` object (in-process) | Generates PGN 0xD6 binary packet (UDP) |
| **Output Port** | N/A (direct memory) | UDP port 15556 |
| **Acceleration** | `isAccelForward`, `isAccelBack` flags | **None** (instant speed changes) |
| **Control Interface** | UI buttons, scrollbar | Command API (MediatR) |
| **Steering Modes** | Manual OR Guidance (auto-steer) | Manual only (no guidance integration) |

---

## Detailed Differences

### 1. Speed Control Architecture

#### Legacy (CSim.cs)
**stepDistance is the primary variable:**

```csharp
// UI directly modifies stepDistance
sim.stepDistance += 0.02;  // Speed up button

// Speed is calculated FROM stepDistance
mf.pn.vtgSpeed = Math.Abs(Math.Round(4 * stepDistance * 10, 2));
```

**Speed formula derivation:**
- Timer: 93ms = 0.093 seconds per tick
- `stepDistance` is in kilometers
- Conversion: `speed_kmh = distance_km / time_sec × 3600`
- Simplified: `speed_kmh = stepDistance / 0.093 × 3600 ≈ 4 × stepDistance × 10` (approximation)

**Acceleration flags:**
```csharp
if (isAccelForward)
{
    stepDistance += 0.02;
    if (stepDistance > 0.12) isAccelForward = false;
}
```

#### New (SimulatorService.cs)
**Speed is the primary variable:**

```csharp
// Commands set speed directly
public void SetSpeed(double speedKmh)
{
    _speedKmh = speedKmh;  // Direct assignment
}

// stepDistance is calculated FROM speed
_stepDistance = (_speedKmh / 3600.0) * 0.093; // Exact calculation
```

**Calculation:**
- Speed in km/h → km/s: `speedKmh / 3600`
- Distance per tick (93ms): `(km/s) × 0.093 = stepDistance`

**No acceleration:**
- Speed changes are instant
- No gradual acceleration/deceleration

---

### 2. Output Mechanism

#### Legacy (CSim.cs:78-94)
**Direct memory writes:**

```csharp
// Update CNMEA object in-process
mf.pn.fix.northing = fixCoord.Northing;
mf.pn.fix.easting = fixCoord.Easting;
mf.pn.headingTrue = glm.toDegrees(headingTrue);
mf.pn.vtgSpeed = Math.Abs(Math.Round(4 * stepDistance * 10, 2));
mf.pn.altitude = SimulateAltitude(CurrentLatLon);
mf.pn.hdop = 0.7;
mf.pn.satellitesTracked = 12;
mf.sentenceCounter = 0;

// Trigger processing
mf.UpdateFixPosition();
```

#### New (SimulatorService.cs:162-229)
**Binary packet generation (UDP):**

```csharp
// Generate PGN 0xD6 packet (57 bytes)
private byte[] GenerateGpsPacket()
{
    byte[] packet = new byte[57];

    // Header: 0x80 0x81 0x7F 0xD6
    packet[0] = 0x80;
    packet[1] = 0x81;
    packet[2] = 0x7F;
    packet[3] = 0xD6;

    // Longitude (bytes 5-12)
    Buffer.BlockCopy(BitConverter.GetBytes(_longitude), 0, packet, 5, 8);

    // Latitude (bytes 13-20)
    Buffer.BlockCopy(BitConverter.GetBytes(_latitude), 0, packet, 13, 8);

    // ... (remaining fields)

    // Checksum (byte 56)
    packet[56] = checksum;

    return packet;
}
```

**Pipeline:**
1. `SimulatorService.Tick()` generates binary packet
2. `SimulatorHostedService` sends via UDP to localhost:15556
3. `UdpPacketReceiver` receives packet
4. `GnssService` unpacks and processes
5. `ApplicationOrchestrator` broadcasts state via SignalR

---

### 3. Geodetic Position Calculation

#### Legacy (CSim.cs:75)
**Uses existing method:**

```csharp
CurrentLatLon = CurrentLatLon.CalculateNewPostionFromBearingDistance(
    headingTrue, stepDistance);
```

This calls a method in the `Wgs84` class (implementation not shown in CSim.cs).

#### New (SimulatorService.cs:123-138)
**Explicit great circle navigation:**

```csharp
private (double lat, double lon) CalculateNewPosition(
    double latDeg, double lonDeg, double bearingRad, double distanceKm)
{
    const double earthRadiusKm = 6371.0;

    double latRad = latDeg * DEG_TO_RAD;
    double lonRad = lonDeg * DEG_TO_RAD;
    double angularDistance = distanceKm / EARTH_RADIUS_KM;

    double newLatRad = Math.Asin(
        Math.Sin(latRad) * Math.Cos(angularDistance) +
        Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

    double newLonRad = lonRad + Math.Atan2(
        Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
        Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad));

    return (newLatRad * RAD_TO_DEG, newLonRad * RAD_TO_DEG);
}
```

**Mathematical equivalence:** Both use spherical trigonometry (great circle navigation). The new implementation makes the math explicit.

---

### 4. Steering Smoothing Algorithm

#### Both Implementations (IDENTICAL)

**Legacy (CSim.cs:33-62):**
```csharp
double diff = Math.Abs(steerAngle - steerangleAve);

if (diff > 11)
{
    if (steerangleAve >= steerAngle)
        steerangleAve -= 6;
    else steerangleAve += 6;
}
else if (diff > 5)
{
    if (steerangleAve >= steerAngle)
        steerangleAve -= 2;
    else steerangleAve += 2;
}
else if (diff > 1)
{
    if (steerangleAve >= steerAngle)
        steerangleAve -= 0.5;
    else steerangleAve += 0.5;
}
else
{
    steerangleAve = steerAngle;
}
```

**New (SimulatorService.cs:84-100):**
```csharp
double diff = Math.Abs(_steerAngle - _steerAngleAve);

if (diff > 11)
{
    _steerAngleAve += (_steerAngle > _steerAngleAve) ? 6 : -6;
}
else if (diff > 5)
{
    _steerAngleAve += (_steerAngle > _steerAngleAve) ? 2 : -2;
}
else if (diff > 1)
{
    _steerAngleAve += (_steerAngle > _steerAngleAve) ? 0.5 : -0.5;
}
else
{
    _steerAngleAve = _steerAngle;
}
```

✓ **Identical logic** (just different syntax: ternary operator vs if/else)

---

### 5. Control Interface

#### Legacy (CSim)
**UI-driven controls:**

- **Speed buttons** ([Controls.Designer.cs:2098-2115](../SourceCode/GPS/Forms/Controls.Designer.cs#L2098-L2115)):
  - `btnSimSpeedUp` - Increase stepDistance
  - `btnSpeedDn` - Decrease stepDistance
  - `btnSimSetSpeedToZero` - Zero speed
  - Acceleration flags: `isAccelForward`, `isAccelBack`

- **Steering scrollbar** ([Controls.Designer.cs:2146-2150](../SourceCode/GPS/Forms/Controls.Designer.cs#L2146-L2150)):
  - Range: 0-800 (center = 400)
  - Maps to: -40° to +40°

- **Direction button** ([Controls.Designer.cs:2134-2145](../SourceCode/GPS/Forms/Controls.Designer.cs#L2134-L2145)):
  - `btnSimReverseDirection` - Reverse heading by 180°

- **Reset buttons**:
  - `btnResetSim` - Reset to saved coordinates
  - `btnResetSteerAngle` - Center steering

- **Automatic steering** ([Controls.Designer.cs:2118-2133](../SourceCode/GPS/Forms/Controls.Designer.cs#L2118-L2133)):
  ```csharp
  if (recPath.isDrivingRecordedPath || isBtnAutoSteerOn && (guidanceLineDistanceOff != 32000))
  {
      lastSimGuidanceAngle = guidanceLineSteerAngle * 0.01 * 0.9;
      sim.DoSimTick(lastSimGuidanceAngle);
  }
  ```

#### New (SimulatorService)
**Command-based API:**

- **Commands** (via MediatR):
  - `StartSimulatorCommand(lat, lon, heading, speed)` - Start with parameters
  - `StopSimulatorCommand()` - Stop simulator
  - `SetSimulatorSpeedCommand(speedKmh)` - Change speed
  - `SetSimulatorSteeringCommand(angle)` - Change steering
  - `ResetSimulatorCommand()` - Reset state

- **No UI controls** - pure backend service
- **No acceleration flags** - instant speed changes
- **No guidance integration** - manual steering only

**Example usage** ([SimulatorIntegrationTests.cs:70-78](../SourceCode/Tests/AgOpenGPS.API.IntegrationTests/SimulatorIntegrationTests.cs#L70-L78)):
```csharp
await _backendClient.SendCommandAsync(
    new StartSimulatorCommand(
        Latitude: 45.0,
        Longitude: -93.0,
        HeadingDegrees: 0.0,
        SpeedKmh: 10.0
    ));
```

---

### 6. Altitude Simulation

#### Both Implementations (IDENTICAL)

**Legacy (CSim.cs:110-122):**
```csharp
private double SimulateAltitude(Wgs84 latLon)
{
    double temp = Math.Abs(latLon.Latitude * 100);
    temp -= ((int)(temp));
    temp *= 100;
    double altitude = temp + 200;

    temp = Math.Abs(latLon.Longitude * 100);
    temp -= ((int)(temp));
    temp *= 100;
    altitude += temp;
    return altitude;
}
```

**New (SimulatorService.cs:143-156):**
```csharp
private double SimulateAltitude(double latitude, double longitude)
{
    double temp = Math.Abs(latitude * 100);
    temp -= (int)temp;
    temp *= 100;
    double altitude = temp + 200;

    temp = Math.Abs(longitude * 100);
    temp -= (int)temp;
    temp *= 100;
    altitude += temp;

    return altitude;
}
```

✓ **Identical algorithm** (creates deterministic pseudo-random altitude: 200-400m range)

---

## Key Architectural Differences

### Legacy Simulator: Tightly Coupled

```
┌─────────────┐
│  FormGPS    │
│  (WinForms) │
└──────┬──────┘
       │
       ├─► timerSim (93ms)
       │   └─► CSim.DoSimTick()
       │       └─► CNMEA object (direct memory)
       │           └─► UpdateFixPosition()
       │               └─► Application logic
       │
       └─► UI Controls (buttons, scrollbar)
           └─► Modify CSim properties
```

**Characteristics:**
- **In-process** - all in one executable
- **Synchronous** - direct method calls
- **Tightly coupled** - CSim has reference to FormGPS (`mf`)
- **UI-driven** - buttons directly modify state

---

### New Simulator: Loosely Coupled (Event-Driven)

```
┌──────────────────┐
│  SimulatorService│
│  (.NET 8 Backend)│
└────────┬─────────┘
         │
         ├─► SimulatorHostedService (93ms timer)
         │   └─► SimulatorService.Tick()
         │       └─► Generate PGN 0xD6 packet
         │           └─► UDP localhost:15556
         │
         ├─► UdpPacketReceiver
         │   └─► GnssService
         │       └─► ApplicationOrchestrator
         │           └─► SignalR broadcast
         │               └─► FormGPS (subscriber)
         │
         └─► MediatR Commands (via SignalR)
             ├─► StartSimulatorCommand
             ├─► SetSimulatorSpeedCommand
             └─► SetSimulatorSteeringCommand
```

**Characteristics:**
- **Cross-process** - backend API + frontend client
- **Asynchronous** - UDP + SignalR
- **Loosely coupled** - no direct references
- **Command-driven** - API commands via MediatR
- **Event-driven** - UDP packets trigger processing

---

## Integration Test Verification

The new simulator has comprehensive integration tests ([SimulatorIntegrationTests.cs](../SourceCode/Tests/AgOpenGPS.API.IntegrationTests/SimulatorIntegrationTests.cs)):

### Test Coverage

1. **Command Dispatch** (Lines 66-214):
   - Start/Stop simulator
   - Change speed dynamically
   - Change steering dynamically
   - Reset simulator state

2. **UDP Pipeline** (Lines 218-288):
   - Verifies UDP packets sent (not direct calls)
   - Validates ~93ms timing
   - Confirms full pipeline processing

3. **Movement Physics** (Lines 292-376):
   - Realistic position changes
   - Curved path with steering
   - Zero speed = no movement

4. **Complex Scenarios** (Lines 380-469):
   - Multiple command sequences
   - Speed affects distance traveled

5. **Integration** (Lines 473-552):
   - AgIO packet format compatibility
   - Multi-client SignalR broadcasts

### Example Test: Movement Verification

```csharp
[Test]
public async Task SimulatorMovement_ShouldShowRealisticPositionChanges()
{
    // Start at 10 km/h heading north for 3.5 seconds
    await _backendClient!.SendCommandAsync(
        new StartSimulatorCommand(45.0, -93.0, 0.0, 10.0));
    await Task.Delay(3500);

    var gpsStates = _receivedStates.Where(s => s.Gnss != null).ToList();
    var firstPos = gpsStates.First().WgsPosition;
    var lastPos = gpsStates.Last().WgsPosition;

    // Expected: 10 km/h × 3.5s = 9.7m north ≈ 0.000087° latitude
    var latChange = lastPos.Latitude - firstPos.Latitude;
    latChange.Should().BeGreaterThan(0.00005);
    latChange.Should().BeLessThan(0.0002);
}
```

**Result:** ✓ Verifies physics calculations are correct

---

## Migration Notes

### What's the Same
✓ Timer interval (93ms)
✓ Steering smoothing algorithm
✓ Heading calculation (bicycle model)
✓ Altitude simulation
✓ GPS quality values (HDOP, satellites)

### What's Different
❌ Speed control (stepDistance → speedKmh)
❌ Output mechanism (CNMEA object → UDP packet)
❌ Control interface (UI buttons → API commands)
❌ Acceleration/deceleration (gradual → instant)
❌ Steering modes (manual/guidance → manual only)

### Future Work

To fully replicate legacy behavior:

1. **Add acceleration/deceleration:**
   ```csharp
   public void AccelerateForward()
   {
       _isAccelerating = true;
       _accelerationTarget = Math.Min(_speedKmh + 5.0, 322.0);
   }

   // In Tick():
   if (_isAccelerating)
   {
       _speedKmh += 0.02 * 43; // Match legacy acceleration rate
       if (_speedKmh >= _accelerationTarget) _isAccelerating = false;
   }
   ```

2. **Add guidance integration:**
   ```csharp
   public void SetAutomaticSteering(double guidanceAngle, bool inDeadZone)
   {
       if (inDeadZone)
           // Hold last angle
       else
           _steerAngle = guidanceAngle * 0.01 * 0.9;
   }
   ```

3. **Add reverse direction:**
   ```csharp
   public void ReverseDirection()
   {
       _headingRad += Math.PI;
       if (_headingRad > TWO_PI) _headingRad -= TWO_PI;
   }
   ```

---

## Conclusion

The new `SimulatorService` successfully replicates the core physics of the legacy `CSim` simulator:
- ✓ Same steering dynamics
- ✓ Same movement calculations
- ✓ Same timing (93ms)

**Key architectural improvements:**
1. **Decoupled** - backend service independent of UI
2. **Testable** - comprehensive integration tests
3. **Cross-platform** - .NET 8 (not .NET Framework)
4. **Event-driven** - UDP pipeline matches real AgIO flow
5. **Command API** - clean interface for external control

**Trade-offs:**
- Lost gradual acceleration/deceleration (can be added back)
- Lost guidance integration (can be added back)
- Lost UI controls (replaced with API commands)

The new simulator is architecturally superior while maintaining physics fidelity. Missing features can be incrementally added as needed.
