# AgOpenGPS Simulator - Detailed Technical Description

## Overview

The AgOpenGPS simulator is an internal GPS/GNSS position simulator that allows testing the application without physical GPS hardware. It simulates vehicle movement, steering dynamics, position updates, and GPS quality metrics.

## Architecture Components

### 1. Core Simulator Class (`CSim.cs`)

**Location**: `SourceCode/GPS/Classes/CSim.cs`

**Key Properties**:
```csharp
- CurrentLatLon: Wgs84          // Current simulated GPS position (lat/lon)
- headingTrue: double           // Vehicle heading in radians (0 to 2π)
- stepDistance: double          // Distance increment per tick (controls speed)
- steerAngle: double            // Target steering angle
- steerangleAve: double         // Smoothed current steering angle
- steerAngleScrollBar: double   // Manual steering input from UI
- isAccelForward: bool          // Acceleration forward flag
- isAccelBack: bool             // Acceleration backward flag
```

**Initialization** ([FormGPS.cs:374](../SourceCode/GPS/Forms/FormGPS.cs#L374)):
```csharp
sim = new CSim(this);
```

The simulator reads initial position from saved settings:
- `setGPS_SimLatitude`
- `setGPS_SimLongitude`

### 2. Timer System

**Timer Configuration** ([FormGPS.Designer.cs:670](../SourceCode/GPS/Forms/FormGPS.Designer.cs#L670)):
```csharp
this.timerSim.Interval = 93;  // Approximately 10.75 Hz
this.timerSim.Tick += timerSim_Tick;
```

**Timer Tick Handler** ([Controls.Designer.cs:2118-2133](../SourceCode/GPS/Forms/Controls.Designer.cs#L2118-L2133)):

The timer determines steering input based on mode:

1. **Auto-Steer or Recorded Path Mode**:
   ```csharp
   if (recPath.isDrivingRecordedPath || isBtnAutoSteerOn && (guidanceLineDistanceOff != 32000))
   {
       if (vehicle.isInDeadZone)
           sim.DoSimTick(lastSimGuidanceAngle);  // Hold last angle
       else
       {
           lastSimGuidanceAngle = guidanceLineSteerAngle * 0.01 * 0.9;
           sim.DoSimTick(lastSimGuidanceAngle);
       }
   }
   ```

2. **Manual Mode**:
   ```csharp
   else
       sim.DoSimTick(sim.steerAngleScrollBar);  // Use manual steering input
   ```

### 3. Simulator Logic - DoSimTick Method

**Location**: [CSim.cs:29-108](../SourceCode/GPS/Classes/CSim.cs#L29-L108)

#### Step 1: Steering Smoothing

The simulator applies **non-linear damping** to smooth steering transitions:

```csharp
double diff = Math.Abs(steerAngle - steerangleAve);

if (diff > 11)
    steerangleAve += (steerangleAve >= steerAngle) ? -6 : +6;
else if (diff > 5)
    steerangleAve += (steerangleAve >= steerAngle) ? -2 : +2;
else if (diff > 1)
    steerangleAve += (steerangleAve >= steerAngle) ? -0.5 : +0.5;
else
    steerangleAve = steerAngle;

mf.mc.actualSteerAngleDegrees = steerangleAve;
```

**Damping rates**:
- Large difference (>11°): ±6° per tick (~64°/sec)
- Medium difference (5-11°): ±2° per tick (~21°/sec)
- Small difference (1-5°): ±0.5° per tick (~5°/sec)
- Minimal difference (<1°): Snap to target

#### Step 2: Heading Calculation

Uses **bicycle model** kinematics:

```csharp
double temp = stepDistance * Math.Tan(steerangleAve * 0.0165329252) / 2;
headingTrue += temp;

// Normalize to [0, 2π]
if (headingTrue > glm.twoPI) headingTrue -= glm.twoPI;
if (headingTrue < 0) headingTrue += glm.twoPI;
```

**Formula**:
```
Δheading = (stepDistance × tan(steerAngle_rad)) / 2
```

The factor of `/2` approximates the vehicle's turn radius.

#### Step 3: Speed Calculation

```csharp
mf.pn.vtgSpeed = Math.Abs(Math.Round(4 * stepDistance * 10, 2));
mf.pn.AverageTheSpeed();
```

**Speed formula**:
```
speed_kmh = |4 × stepDistance × 10|
```

With 93ms tick interval (10.75 Hz):
- `stepDistance = 0.1` → ~4.3 km/h
- `stepDistance = 1.0` → ~43 km/h
- `stepDistance = 7.5` (max) → ~322 km/h

#### Step 4: Position Update

Uses **geodetic calculations**:

```csharp
CurrentLatLon = CurrentLatLon.CalculateNewPostionFromBearingDistance(
    headingTrue, stepDistance);
```

This method:
1. Takes current WGS84 position (lat/lon)
2. Calculates new position based on bearing (heading) and distance
3. Returns new WGS84 coordinates

#### Step 5: Coordinate Conversion

Converts WGS84 to local Cartesian coordinates:

```csharp
GeoCoord fixCoord = mf.AppModel.LocalPlane.ConvertWgs84ToGeoCoord(CurrentLatLon);
mf.pn.fix.northing = fixCoord.Northing;
mf.pn.fix.easting = fixCoord.Easting;
```

#### Step 6: Update GPS State

Sets all GPS-related values:

```csharp
// Position
mf.pn.fix.northing = fixCoord.Northing;
mf.pn.fix.easting = fixCoord.Easting;
mf.AppModel.CurrentLatLon = CurrentLatLon;

// Heading (degrees)
mf.pn.headingTrue = glm.toDegrees(headingTrue);
mf.pn.headingTrueDual = mf.pn.headingTrue;
mf.ahrs.imuHeading = mf.pn.headingTrue;
if (mf.ahrs.imuHeading >= 360) mf.ahrs.imuHeading -= 360;

// GPS Quality
mf.pn.hdop = 0.7;                    // Horizontal dilution of precision
mf.pn.altitude = SimulateAltitude(CurrentLatLon);
mf.pn.satellitesTracked = 12;

// Reset sentence counter (marks GPS as "connected")
mf.sentenceCounter = 0;
```

#### Step 7: Acceleration Logic

Handles transient acceleration/deceleration:

```csharp
if (isAccelForward)
{
    isAccelBack = false;
    stepDistance += 0.02;
    if (stepDistance > 0.12) isAccelForward = false;  // Stop at 5.2 km/h
}
if (isAccelBack)
{
    isAccelForward = false;
    stepDistance -= 0.01;
    if (stepDistance < -0.06) isAccelBack = false;  // Stop at -2.6 km/h
}
```

#### Step 8: Trigger Application Update

```csharp
mf.UpdateFixPosition();
```

This is the **main application hook** that processes the simulated GPS data through the normal GPS update pipeline.

### 4. Altitude Simulation

**Method**: [CSim.cs:110-122](../SourceCode/GPS/Classes/CSim.cs#L110-L122)

Creates a **deterministic pseudo-random altitude** based on position:

```csharp
private double SimulateAltitude(Wgs84 latLon)
{
    // Extract fractional part of latitude (×100)
    double temp = Math.Abs(latLon.Latitude * 100);
    temp -= ((int)(temp));
    temp *= 100;
    double altitude = temp + 200;  // Base altitude 200m

    // Add fractional part of longitude (×100)
    temp = Math.Abs(latLon.Longitude * 100);
    temp -= ((int)(temp));
    temp *= 100;
    altitude += temp;

    return altitude;  // Range: ~200-400m
}
```

This creates smooth altitude variations as the vehicle moves.

## UI Controls - Simulator Panel

**Panel**: `panelSim` ([FormGPS.cs:445-447](../SourceCode/GPS/Forms/FormGPS.cs#L445-L447))

### Control Layout

```
┌──────────────────────────────────────────────────────┐
│ [Reset] [>0<]    [═══════════]    [-][■][+]  [↻]    │
└──────────────────────────────────────────────────────┘
   btnResetSim      hsbarSteerAngle  Speed    Reverse
   btnResetSteerAngle               Controls
```

### Button Controls

1. **btnResetSim** - Reset Position ([Controls.Designer.cs:2157-2162](../SourceCode/GPS/Forms/Controls.Designer.cs#L2157-L2162)):
   ```csharp
   sim.CurrentLatLon = new Wgs84(
       Properties.Settings.Default.setGPS_SimLatitude,
       Properties.Settings.Default.setGPS_SimLongitude);
   ```

2. **btnResetSteerAngle** - Center Steering ([Controls.Designer.cs:2151-2156](../SourceCode/GPS/Forms/Controls.Designer.cs#L2151-L2156)):
   ```csharp
   sim.steerAngleScrollBar = 0;
   hsbarSteerAngle.Value = 400;  // Center position
   ```

3. **hsbarSteerAngle** - Manual Steering Control:
   - Range: 0-800 (center = 400)
   - Maps to: -40° to +40° steering angle
   ```csharp
   sim.steerAngleScrollBar = (hsbarSteerAngle.Value - 400) * 0.1;
   ```

4. **btnSimSpeedUp** - Increase Speed ([Controls.Designer.cs:2098-2109](../SourceCode/GPS/Forms/Controls.Designer.cs#L2098-L2109)):
   ```csharp
   if (sim.stepDistance < 0)
       sim.stepDistance = 0;  // Reverse → Stop
   else if (sim.stepDistance < 0.2)
       sim.stepDistance += 0.02;  // +0.86 km/h
   else
       sim.stepDistance *= 1.15;  // +15%

   if (sim.stepDistance > 7.5) sim.stepDistance = 7.5;  // Max ~322 km/h
   ```

5. **btnSpeedDn** - Decrease Speed ([Controls.Designer.cs:2110-2115](../SourceCode/GPS/Forms/Controls.Designer.cs#L2110-L2115)):
   ```csharp
   if (sim.stepDistance < 0.2 && sim.stepDistance > -0.51)
       sim.stepDistance -= 0.02;  // -0.86 km/h
   else
       sim.stepDistance *= 0.8;  // -20%

   if (sim.stepDistance < -0.5) sim.stepDistance = -0.5;  // Max reverse
   ```

6. **btnSimSetSpeedToZero** - Emergency Stop ([Controls.Designer.cs:2163-2166](../SourceCode/GPS/Forms/Controls.Designer.cs#L2163-L2166)):
   ```csharp
   sim.stepDistance = 0;
   ```

7. **btnSimReverseDirection** - 180° Turn ([Controls.Designer.cs:2134-2145](../SourceCode/GPS/Forms/Controls.Designer.cs#L2134-L2145)):
   ```csharp
   sim.headingTrue += Math.PI;  // +180°
   ABLine.isABValid = false;     // Invalidate guidance
   curve.isCurveValid = false;
   if (isBtnAutoSteerOn)
       btnAutoSteer.PerformClick();  // Disable auto-steer
   ```

## Enabling/Disabling Simulator

### Menu Item

**Location**: File Menu → "Simulator On" (checkbox)

**Handler**: [Controls.Designer.cs:1462-1482](../SourceCode/GPS/Forms/Controls.Designer.cs#L1462-L1482)

```csharp
private void simulatorOnToolStripMenuItem_Click(object sender, EventArgs e)
{
    // Prevent enabling during active job
    if (isJobStarted)
    {
        TimedMessageBox(2000, "Field Is Open", "Close Field First");
        return;
    }

    // Prevent disabling if GPS is connected
    if (simulatorOnToolStripMenuItem.Checked)
    {
        if (sentenceCounter < 299)  // GPS is active
        {
            TimedMessageBox(2000, "Connected", "GPS");
            simulatorOnToolStripMenuItem.Checked = false;
            return;
        }
    }

    // Toggle simulator
    timerSim.Enabled = panelSim.Visible = simulatorOnToolStripMenuItem.Checked;

    // Reset GPS initialization flags
    isFirstFixPositionSet = false;
    isGPSPositionInitialized = false;
    isFirstHeadingSet = false;
    // ... (additional resets)
}
```

**State synchronization**:
- `timerSim.Enabled` = simulator active
- `panelSim.Visible` = UI controls visible
- `simulatorOnToolStripMenuItem.Checked` = menu checkbox state

### Setting Initial Coordinates

**Menu**: File Menu → "Enter Sim Coords"

**Handler**: [Controls.Designer.cs:1375-1381](../SourceCode/GPS/Forms/Controls.Designer.cs#L1375-L1381)

Opens `FormSimCoords` dialog to set:
- `setGPS_SimLatitude`
- `setGPS_SimLongitude`

## Integration with Main Application

### GPS Data Pipeline

The simulator writes to the **CNMEA parser object** (`pn`):

```
Simulator (CSim)
    ↓
CNMEA object (pn)
    ↓ calls
UpdateFixPosition()
    ↓
Application processing
(guidance, section control, rendering)
```

**Key fields updated**:
- `pn.fix.easting`, `pn.fix.northing` - Local coordinates
- `pn.speed`, `pn.vtgSpeed` - Speed in km/h
- `pn.headingTrue`, `pn.headingTrueDual` - Heading in degrees
- `pn.altitude` - Altitude in meters
- `pn.hdop` - Horizontal dilution (fixed at 0.7)
- `pn.satellitesTracked` - Satellite count (fixed at 12)
- `pn.fixQuality` - GPS fix quality
- `sentenceCounter` - Reset to 0 (marks GPS as "active")

### Module Communication

The simulated steering angle is also published:

```csharp
mf.mc.actualSteerAngleDegrees = steerangleAve;
```

This allows the rest of the system to see the simulated steering response.

## Backend Migration Notes

### Current Architecture (Legacy)

**Timer-driven** ([FormGPS.cs:575,583](../SourceCode/GPS/Forms/FormGPS.cs#L575)):
```csharp
timerSim.Interval = 93;  // 93ms = ~10.75 Hz
timerSim.Tick += timerSim_Tick;
```

### New Architecture (Backend-Driven)

**Backend integration** ([FormGPS.cs:549-585](../SourceCode/GPS/Forms/FormGPS.cs#L549-L585)):

When backend connects:
```csharp
await _backendClient.ConnectAsync();
timerSim.Enabled = false;  // Disable timer
```

Backend pushes `ApplicationState` at 4 Hz (250ms) via SignalR.

**Important**: The legacy `timerSim` still runs at 93ms for local simulator when backend is unavailable. This allows development/testing without backend.

## Mathematical Constants

### Conversion Factors
- `0.0165329252` = π/180 (degrees to radians)
- `glm.twoPI` = 2π (6.28318...)
- `glm.toDegrees(rad)` = rad × 180/π

### Speed Conversion
With 93ms ticks and formula `speed = |4 × stepDistance × 10|`:
- Factor `4` accounts for ~93ms → 1 second conversion
- Factor `10` converts to km/h scale

## Summary of Key Parameters

| Parameter | Value | Description |
|-----------|-------|-------------|
| Timer interval | 93 ms | ~10.75 Hz update rate |
| Max forward speed | `stepDistance = 7.5` | ~322 km/h |
| Max reverse speed | `stepDistance = -0.5` | ~21 km/h |
| Steering range | ±40° | Manual control range |
| Steering smoothing | Variable | 0.5-6°/tick depending on error |
| GPS quality (HDOP) | 0.7 | Fixed "good" value |
| Satellites tracked | 12 | Fixed value |
| Altitude range | 200-400m | Pseudo-random based on position |

## Recreation Checklist

To recreate this simulator from scratch:

1. **Create CSim class** with position, heading, speed state
2. **Implement DoSimTick()** with:
   - Steering smoothing (non-linear damping)
   - Bicycle model heading update
   - Geodetic position calculation (WGS84)
   - Coordinate conversion (WGS84 → local plane)
   - Speed calculation
   - GPS quality metrics
3. **Create 93ms timer** to call DoSimTick periodically
4. **Add UI panel** with:
   - Speed control buttons (up/down/stop)
   - Manual steering scrollbar (-40° to +40°)
   - Reset buttons (position, steering)
   - Reverse direction button
5. **Integrate with GPS pipeline** by writing to NMEA parser fields
6. **Add menu toggle** to enable/disable simulator
7. **Implement coordinate entry** dialog for starting position
8. **Handle auto-steer mode** by feeding guidance angle to simulator
9. **Prevent conflicts** with real GPS (check sentence counter)
10. **Backend migration**: Replace timer with state-push architecture

---

This simulator provides a complete virtual GPS system for testing AgOpenGPS functionality without physical hardware. The geodetic calculations, steering dynamics, and GPS quality simulation make it suitable for realistic testing scenarios.
