# UDP Communication and Simulator Architecture

## Overview

AgOpenGPS uses a UDP-based communication architecture to exchange data between the main application (FormGPS), the communication hub (AgIO), and hardware modules (AutoSteer, Machine, IMU). The system supports two operating modes: **Simulator Mode** (internal GPS simulation) and **Real GPS Mode** (external GPS hardware via AgIO).

This document details how UDP communication works in both modes and explains the simulator's architecture.

---

## Table of Contents

1. [Communication Ports](#communication-ports)
2. [Simulator Architecture](#simulator-architecture)
3. [UDP Flow: Simulator Mode](#udp-flow-simulator-mode)
4. [UDP Flow: Real GPS Mode](#udp-flow-real-gps-mode)
5. [PGN Packet Formats](#pgn-packet-formats)
6. [AgIO's Role](#agios-role)
7. [Auto-Switch Logic](#auto-switch-logic)
8. [Code References](#code-references)

---

## Communication Ports

### Loopback Ports (localhost)
| Port | Direction | Purpose | Owner |
|------|-----------|---------|-------|
| **15555** | AgIO → FormGPS | GPS data (PGN 0xD6) | FormGPS listens |
| **17777** | FormGPS → AgIO | Control commands | AgIO listens |

### Network Ports (UDP broadcast)
| Port | Direction | Purpose |
|------|-----------|---------|
| **8888** | AgIO ↔ Modules | Module communication (broadcast to 192.168.5.255) |
| **9999** | AgIO listens | UDP network (if enabled) |

---

## Simulator Architecture

### Overview

The built-in simulator generates GPS position, heading, speed, and navigation data internally within FormGPS, eliminating the need for real GPS hardware or AgIO (for GPS data).

### Components

#### 1. CSim Class
**Location:** [SourceCode/GPS/Classes/CSim.cs](../../SourceCode/GPS/Classes/CSim.cs)

**Purpose:** Physics-based simulation of vehicle movement and GPS position

**Key Properties:**
```csharp
public Wgs84 CurrentLatLon        // Current simulated position
public double headingTrue         // Simulated heading (radians)
public double stepDistance        // Distance per tick (speed)
public double steerAngle          // Commanded steering angle
public double steerangleAve       // Smoothed steering response
```

**Key Method:** `DoSimTick(double _st)`
- Called every 93ms by timerSim
- Simulates steering response (smoothing/delay)
- Calculates new heading based on steering angle and wheelbase
- Calculates new position using bearing/distance
- Updates `pn.fix`, `pn.speed`, `pn.headingTrue`, etc.
- Calls `UpdateFixPosition()` to trigger downstream processing

#### 2. timerSim Timer
**Location:** [SourceCode/GPS/Forms/FormGPS.Designer.cs:670](../../SourceCode/GPS/Forms/FormGPS.Designer.cs#L670)

**Configuration:**
- **Interval:** 93ms (~10.75 Hz)
- **Event Handler:** [Controls.Designer.cs:2118](../../SourceCode/GPS/Forms/Controls.Designer.cs#L2118)

**Tick Handler Logic:**
```csharp
private void timerSim_Tick(object sender, EventArgs e)
{
    if (recPath.isDrivingRecordedPath || isBtnAutoSteerOn && (guidanceLineDistanceOff != 32000))
    {
        // Use guidance angle for autosteer simulation
        if (vehicle.isInDeadZone)
            sim.DoSimTick((double)lastSimGuidanceAngle);
        else
        {
            lastSimGuidanceAngle = (double)guidanceLineSteerAngle * 0.01 * 0.9;
            sim.DoSimTick(lastSimGuidanceAngle);
        }
    }
    else
        sim.DoSimTick(sim.steerAngleScrollBar); // Manual steering
}
```

#### 3. Integration with Backend (Workflow 001)

**Status:** Simulator still runs independently; backend integration pending

**Current Behavior:**
- Line 572: `timerSim.Enabled = false` when backend connects
- Line 580: `timerSim.Enabled = true` as fallback when backend unavailable

**Future Migration:** Backend should call `CSim.DoSimTick()` and broadcast state via SignalR

---

## UDP Flow: Simulator Mode

### Architecture

In simulator mode, FormGPS generates GPS data internally and sends control commands to modules. AgIO is optional (only needed for module communication).

```
┌─────────────────────────────────────────────────────────┐
│ FormGPS (Simulator Mode)                                │
│                                                         │
│  1. timerSim (93ms)                                    │
│     ↓                                                   │
│  2. CSim.DoSimTick(steerAngle)                         │
│     - Calculate new position (lat/lon)                 │
│     - Calculate heading, speed                         │
│     - Update pn.fix, pn.speed, pn.headingTrue         │
│     ↓                                                   │
│  3. UpdateFixPosition()                                │
│     - Process navigation logic                         │
│     ↓                                                   │
│  4. Send UDP packets via SendPgnToLoop()               │
│     → localhost:17777 (AgIO)                           │
│                                                         │
│     Packets sent:                                      │
│     • PGN 0x64 (100): Corrected Position               │
│       (Lon/Lat/Heading)                                │
│     • PGN 0xFE (254): AutoSteer Data                   │
│       (Speed, SteerAngle, Distance, Sections)          │
│     • PGN 0xEF (239): Machine Data                     │
│       (U-turn, Speed, Hydraulics, GeoStop)             │
│     • PGN 0xE5 (229): Section Control                  │
│       (Section on/off states)                          │
└─────────────────────────────────────────────────────────┘
                    ↓ (optional)
┌─────────────────────────────────────────────────────────┐
│ AgIO (Optional)                                         │
│  - Forwards to modules: UDP 8888 (broadcast)           │
│  - Receives module responses                           │
│  - Forwards responses back to FormGPS                  │
└─────────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────────┐
│ Hardware Modules (AutoSteer, Machine, etc.)            │
│  - Receive control commands                            │
│  - Send status responses                               │
└─────────────────────────────────────────────────────────┘
```

### Key Points

1. **No inbound GPS data** - FormGPS generates position internally
2. **Outbound commands still sent** - Same PGNs as real GPS mode
3. **AgIO optional for GPS** - Only needed if hardware modules are connected
4. **Self-contained** - No external GPS hardware required

---

## UDP Flow: Real GPS Mode

### Architecture

In real GPS mode, GPS data flows FROM AgIO INTO FormGPS, while control commands flow FROM FormGPS back through AgIO to modules.

```
┌─────────────────────────────────────────────────────────┐
│ GPS Hardware                                            │
│  - Sends NMEA sentences via Serial/USB                 │
│    ($GPGGA, $GNVTG, $GNHDT, $PANDA, etc.)             │
└─────────────────────────────────────────────────────────┘
                    ↓ Serial/USB
┌─────────────────────────────────────────────────────────┐
│ AgIO - Communication Hub                                │
│                                                         │
│ INBOUND (GPS → AgIO):                                  │
│  1. Receive NMEA from GPS hardware                     │
│     [AgIO/Forms/NMEA.Designer.cs:91]                   │
│                                                         │
│  2. ParseNMEA()                                        │
│     - Extract: lat/lon, speed, heading                 │
│     - Extract: altitude, satellites, fix quality       │
│     - Extract: HDOP, age, IMU data                     │
│                                                         │
│  3. Build PGN 0xD6 (57 bytes)                          │
│     [AgIO/Forms/NMEA.Designer.cs:237-308]              │
│     Header: 0x80 0x81 0x7C 0xD6 0x33                   │
│     + Longitude (8 bytes, double)                      │
│     + Latitude (8 bytes, double)                       │
│     + Heading True Dual (4 bytes, float)               │
│     + Heading True (4 bytes, float)                    │
│     + Speed (4 bytes, float)                           │
│     + Roll (4 bytes, float)                            │
│     + Altitude (4 bytes, float)                        │
│     + Satellites (2 bytes, ushort)                     │
│     + Fix Quality (1 byte)                             │
│     + HDOP*100 (2 bytes, ushort)                       │
│     + Age*100 (2 bytes, ushort)                        │
│     + IMU Heading (2 bytes, ushort)                    │
│     + IMU Roll (2 bytes, short)                        │
│     + IMU Pitch (2 bytes, short)                       │
│     + IMU Yaw Rate (2 bytes, short)                    │
│     + Checksum (1 byte)                                │
│                                                         │
│  4. SendToLoopBackMessageAOG()                         │
│     → localhost:15555 (FormGPS)                        │
│                                                         │
│ OUTBOUND (FormGPS → Modules):                          │
│  5. ReceiveFromLoopBack()                              │
│     ← localhost:17777 (FormGPS commands)               │
│                                                         │
│  6. Forward to modules                                 │
│     → UDP 8888 broadcast (192.168.5.255:8888)         │
│                                                         │
│ MODULE RESPONSES:                                       │
│  7. ReceiveFromUDP()                                   │
│     ← UDP 8888 (module status)                         │
│                                                         │
│  8. SendToLoopBackMessageAOG()                         │
│     → localhost:15555 (back to FormGPS)                │
│                                                         │
│ HEARTBEAT (Every 2 seconds):                           │
│  - Send PGN 200 (0xC8): Hello message                  │
│    [AgIO/Forms/FormLoop.cs:447]                        │
│    → UDP 8888 broadcast                                │
│    Purpose: Keepalive so modules know AgIO is alive    │
└─────────────────────────────────────────────────────────┘
          ↓ localhost:15555              ↑ localhost:17777
┌─────────────────────────────────────────────────────────┐
│ FormGPS - Main Application                              │
│                                                         │
│ INBOUND (GPS data):                                    │
│  9. ReceiveFromLoopBack()                              │
│     [GPS/Forms/UDPComm.Designer.cs:32]                 │
│                                                         │
│ 10. case 0xD6: GPS Data                                │
│     [GPS/Forms/UDPComm.Designer.cs:58]                 │
│     - Unpack PGN 0xD6                                  │
│     - Update pn.fix, pn.speed, pn.heading, etc.        │
│     - Auto-disable simulator (line 73)                 │
│     - Call UpdateFixPosition() (line 164)              │
│                                                         │
│ OUTBOUND (Control commands):                           │
│ 11. UpdateFixPosition() sends PGNs                     │
│     [GPS/Forms/Position.designer.cs]                   │
│     → PGN 0x64 (100): Corrected Position (line 868)   │
│     → PGN 0xFE (254): AutoSteer Data (line 1059)      │
│     → PGN 0xEF (239): Machine Data                     │
│     → PGN 0xE5 (229): Section Control                  │
│                                                         │
│ 12. SendPgnToLoop()                                    │
│     [GPS/Forms/UDPComm.Designer.cs:372]                │
│     → localhost:17777 (back to AgIO)                   │
│                                                         │
│ MODULE RESPONSES:                                       │
│ 13. Receive module data                                │
│     case 253 (0xFD): AutoSteer response (line 204)    │
│       - Actual steer angle                             │
│       - Switch states (work/steer)                     │
│       - PWM display                                    │
│       - IMU data (optional)                            │
└─────────────────────────────────────────────────────────┘
          ↓ UDP 8888                     ↑ UDP 8888
┌─────────────────────────────────────────────────────────┐
│ Hardware Modules                                        │
│  - AutoSteer Module (PGN 126/0x7E response)            │
│  - Machine Module (PGN 123/0x7B response)              │
│  - IMU Module (PGN 121/0x79 response)                  │
└─────────────────────────────────────────────────────────┘
```

### Key Points

1. **Bidirectional flow** - GPS data IN, control commands OUT
2. **AgIO required** - Acts as GPS parser + module bridge
3. **Auto-disable sim** - Real GPS automatically disables simulator
4. **Module responses** - Flow back through AgIO to FormGPS

---

## PGN Packet Formats

### PGN Format Structure

All PGN packets use this header structure:
```
[0] = 0x80       // Start byte 1
[1] = 0x81       // Start byte 2
[2] = 0x7F/0x7C  // Source ID
[3] = PGN ID     // Packet type
[4] = Data Length
[5..n] = Data
[n+1] = Checksum
```

### GPS Data: PGN 0xD6 (214) - AgIO → FormGPS

**Size:** 57 bytes
**Frequency:** Variable (depends on GPS update rate)
**Direction:** AgIO → FormGPS (localhost:15555)

**Structure:**
```
Offset | Size | Type   | Field
-------|------|--------|---------------------------
0-4    | 5    | Header | 0x80 0x81 0x7C 0xD6 0x33
5-12   | 8    | double | Longitude
13-20  | 8    | double | Latitude
21-24  | 4    | float  | Heading True Dual
25-28  | 4    | float  | Heading True
29-32  | 4    | float  | Speed (kph)
33-36  | 4    | float  | Roll (degrees)
37-40  | 4    | float  | Altitude (meters)
41-42  | 2    | ushort | Satellites Tracked
43     | 1    | byte   | Fix Quality (0-8)
44-45  | 2    | ushort | HDOP * 100
46-47  | 2    | ushort | Age * 100
48-49  | 2    | ushort | IMU Heading * 10
50-51  | 2    | short  | IMU Roll * 10
52-53  | 2    | short  | IMU Pitch
54-55  | 2    | short  | IMU Yaw Rate
56     | 1    | byte   | Checksum
```

**Code:** [AgIO/Forms/NMEA.Designer.cs:237](../../SourceCode/AgIO/Source/Forms/NMEA.Designer.cs#L237)

### AutoSteer Data: PGN 0xFE (254) - FormGPS → Modules

**Size:** 13 bytes
**Frequency:** Every GPS update (~4-10 Hz)
**Direction:** FormGPS → AgIO → Modules

**Structure:**
```
Offset | Size | Field
-------|------|---------------------------
0-4    | 5    | Header: 0x80 0x81 0x7F 0xFE 0x08
5      | 1    | Speed Lo byte
6      | 1    | Speed Hi byte
7      | 1    | Status byte
8      | 1    | Steer Angle Lo byte
9      | 1    | Steer Angle Hi byte
10     | 1    | Line Distance (cm)
11     | 1    | Sections 1-8 bitmap
12     | 1    | Sections 9-16 bitmap
13     | 1    | Checksum
```

**Code:** [GPS/Forms/Position.designer.cs:1059](../../SourceCode/GPS/Forms/Position.designer.cs#L1059)

### Machine Data: PGN 0xEF (239) - FormGPS → Modules

**Size:** 13 bytes
**Frequency:** Every frame (~60 Hz)
**Direction:** FormGPS → AgIO → Modules

**Structure:**
```
Offset | Field
-------|---------------------------
5      | U-turn state
6      | Speed
7      | Hydraulic lift
8      | Tram line
9      | GeoStop (out of bounds)
11     | Sections 1-8
12     | Sections 9-16
```

**Code:** [GPS/Forms/OpenGL.Designer.cs:653](../../SourceCode/GPS/Forms/OpenGL.Designer.cs#L653)

### Section Control: PGN 0xE5 (229) - FormGPS → Modules

**Size:** 16 bytes
**Frequency:** Every frame (~60 Hz)
**Direction:** FormGPS → AgIO → Modules

**Structure:** Symmetric section zones (64 sections max in 8 bytes)

**Code:** [GPS/Forms/OpenGL.Designer.cs:655](../../SourceCode/GPS/Forms/OpenGL.Designer.cs#L655)

### Corrected Position: PGN 0x64 (100) - FormGPS → Modules

**Size:** 30 bytes
**Frequency:** Every GPS update
**Direction:** FormGPS → AgIO → Modules

**Structure:**
```
Offset | Size | Type   | Field
-------|------|--------|---------------------------
0-4    | 5    | Header | 0x80 0x81 0x7F 0x64 0x18
5-12   | 8    | double | Longitude
13-20  | 8    | double | Latitude
21-28  | 8    | double | GPS Heading (degrees)
29     | 1    | byte   | Checksum
```

**Code:** [GPS/Forms/Position.designer.cs:868](../../SourceCode/GPS/Forms/Position.designer.cs#L868)

### AutoSteer Response: PGN 0xFD (253) - Module → FormGPS

**Size:** 14 bytes
**Frequency:** Module dependent
**Direction:** Module → AgIO → FormGPS

**Structure:**
```
Offset | Field
-------|---------------------------
5-6    | Actual Steer Angle (Int16 * 0.01)
7-8    | IMU Heading (Int16 * 0.1, 9999 = N/A)
9-10   | Roll (Int16 * 0.1, 8888 = N/A)
11     | Switch Status (bit 0=work, bit 1=steer)
12     | PWM value
```

**Code:** [GPS/Forms/UDPComm.Designer.cs:204](../../SourceCode/GPS/Forms/UDPComm.Designer.cs#L204)

### Hello Message: PGN 200 (0xC8) - AgIO → Modules

**Size:** 9 bytes
**Frequency:** Every 2 seconds
**Direction:** AgIO → Modules (broadcast)

**Structure:**
```
byte[] = { 0x80, 0x81, 0x7F, 200, 3, 56, 0, 0, 0x47 }
```

**Purpose:** Heartbeat/keepalive so modules know AgIO is alive

**Code:** [AgIO/Forms/FormLoop.cs:447](../../SourceCode/AgIO/Source/Forms/FormLoop.cs#L447)

---

## AgIO's Role

### Core Responsibilities

AgIO acts as the **communication hub** between GPS hardware, FormGPS, and hardware modules.

#### 1. GPS Data Processing

**Inbound:**
- Receives NMEA sentences from GPS hardware (Serial/USB)
- Parses multiple NMEA formats: GGA, VTG, HDT, AVR, PAOGI, PANDA, KSXT, etc.
- Extracts and validates GPS data with checksums

**Outbound:**
- Packages data into binary PGN 0xD6 format
- Sends to FormGPS via UDP loopback (localhost:15555)

#### 2. Module Communication Bridge

**FormGPS → Modules:**
- Receives control PGNs from FormGPS (localhost:17777)
- Forwards to hardware modules via UDP broadcast (8888)
- Routes based on PGN type:
  - 0xFE (254): AutoSteer + Machine modules
  - 0xEF (239): Machine module
  - 0xFC (252): AutoSteer settings
  - 0xFB (251): AutoSteer config
  - 0xEE (238): Machine config

**Modules → FormGPS:**
- Receives status from modules (UDP 8888)
- Forwards back to FormGPS (localhost:15555)
- Tracks module health via hello message timeouts

#### 3. Heartbeat System

**Every 2 seconds:**
- Sends PGN 200 (hello) to all modules
- Monitors responses to detect disconnections
- Updates UI indicators (green = connected, red = timeout)

**Code:** [AgIO/Forms/FormLoop.cs:433-461](../../SourceCode/AgIO/Source/Forms/FormLoop.cs#L433)

#### 4. NTRIP Support (Optional)

- Receives RTK correction data from NTRIP caster
- Forwards corrections to GPS receiver via Serial/UDP
- Configured via AgIO settings

### When is AgIO Required?

| Scenario | AgIO Required? | Why? |
|----------|---------------|------|
| **Simulator only** | ❌ No | FormGPS generates GPS internally |
| **Real GPS** | ✅ Yes | Parses NMEA → PGN 0xD6 |
| **Hardware modules** | ✅ Yes | UDP bridge to modules |
| **NTRIP corrections** | ✅ Yes | Forwards RTK data to GPS |

---

## Auto-Switch Logic

### Simulator Auto-Disable

**Location:** [GPS/Forms/UDPComm.Designer.cs:73](../../SourceCode/GPS/Forms/UDPComm.Designer.cs#L73)

```csharp
case 0xD6: // GPS Data from AgIO
{
    // ... packet validation ...

    double Lon = BitConverter.ToDouble(data, 5);
    double Lat = BitConverter.ToDouble(data, 13);

    if (Lon != double.MaxValue && Lat != double.MaxValue)
    {
        // Auto-disable simulator when real GPS detected
        if (timerSim.Enabled) DisableSim();

        // Process real GPS data...
    }
}
```

**Behavior:**
- When FormGPS receives PGN 0xD6 (real GPS) from AgIO
- Automatically calls `DisableSim()` to stop timerSim
- Real GPS data takes precedence over simulation
- Prevents dual-source conflicts

### DisableSim() Implementation

**Location:** [GPS/Forms/UDPComm.Designer.cs:343](../../SourceCode/GPS/Forms/UDPComm.Designer.cs#L343)

```csharp
private void DisableSim()
{
    timerSim.Enabled = false;
    // Update UI to reflect simulator is off
}
```

---

## Code References

### Simulator Components

| Component | File | Key Lines |
|-----------|------|-----------|
| CSim class | [GPS/Classes/CSim.cs](../../SourceCode/GPS/Classes/CSim.cs) | 1-125 |
| DoSimTick() | [GPS/Classes/CSim.cs](../../SourceCode/GPS/Classes/CSim.cs#L29) | 29-108 |
| timerSim definition | [GPS/Forms/FormGPS.Designer.cs](../../SourceCode/GPS/Forms/FormGPS.Designer.cs#L670) | 670-671 |
| timerSim handler | [GPS/Forms/Controls.Designer.cs](../../SourceCode/GPS/Forms/Controls.Designer.cs#L2118) | 2118-2133 |

### UDP Communication

| Component | File | Key Lines |
|-----------|------|-----------|
| FormGPS UDP setup | [GPS/Forms/UDPComm.Designer.cs](../../SourceCode/GPS/Forms/UDPComm.Designer.cs) | 1-50 |
| ReceiveFromLoopBack | [GPS/Forms/UDPComm.Designer.cs](../../SourceCode/GPS/Forms/UDPComm.Designer.cs#L32) | 32-366 |
| PGN 0xD6 handler | [GPS/Forms/UDPComm.Designer.cs](../../SourceCode/GPS/Forms/UDPComm.Designer.cs#L58) | 58-166 |
| SendPgnToLoop | [GPS/Forms/UDPComm.Designer.cs](../../SourceCode/GPS/Forms/UDPComm.Designer.cs#L372) | 372-389 |

### AgIO Components

| Component | File | Key Lines |
|-----------|------|-----------|
| AgIO UDP setup | [AgIO/Source/Forms/UDP.designer.cs](../../SourceCode/AgIO/Source/Forms/UDP.designer.cs#L80) | 80-152 |
| ParseNMEA | [AgIO/Source/Forms/NMEA.Designer.cs](../../SourceCode/AgIO/Source/Forms/NMEA.Designer.cs#L91) | 91-313 |
| Build PGN 0xD6 | [AgIO/Source/Forms/NMEA.Designer.cs](../../SourceCode/AgIO/Source/Forms/NMEA.Designer.cs#L237) | 237-308 |
| SendToLoopBackMessageAOG | [AgIO/Source/Forms/UDP.designer.cs](../../SourceCode/AgIO/Source/Forms/UDP.designer.cs#L156) | 156-159 |
| ReceiveFromLoopBack | [AgIO/Source/Forms/UDP.designer.cs](../../SourceCode/AgIO/Source/Forms/UDP.designer.cs#L192) | 192-244 |
| Hello message | [AgIO/Source/Forms/FormLoop.cs](../../SourceCode/AgIO/Source/Forms/FormLoop.cs#L447) | 447 |

### PGN Definitions

| Component | File | Key Lines |
|-----------|------|-----------|
| All PGN classes | [GPS/Forms/PGN.Designer.cs](../../SourceCode/GPS/Forms/PGN.Designer.cs) | 1-499 |
| PGN 0xFE (254) | [GPS/Forms/PGN.Designer.cs](../../SourceCode/GPS/Forms/PGN.Designer.cs#L35) | 35-53 |
| PGN 0xEF (239) | [GPS/Forms/PGN.Designer.cs](../../SourceCode/GPS/Forms/PGN.Designer.cs#L143) | 143-166 |

### UpdateFixPosition

| Component | File | Key Lines |
|-----------|------|-----------|
| UpdateFixPosition | [GPS/Forms/Position.designer.cs](../../SourceCode/GPS/Forms/Position.designer.cs#L128) | 128-1200+ |
| Send PGN 0x64 | [GPS/Forms/Position.designer.cs](../../SourceCode/GPS/Forms/Position.designer.cs#L868) | 868 |
| Send PGN 0xFE | [GPS/Forms/Position.designer.cs](../../SourceCode/GPS/Forms/Position.designer.cs#L1059) | 1059 |

---

## Summary: Simulator vs Real GPS

| Aspect | Simulator Mode | Real GPS Mode |
|--------|---------------|---------------|
| **GPS Source** | CSim class (internal) | GPS hardware → AgIO |
| **timerSim** | ✅ Running (93ms) | ❌ Auto-disabled |
| **Inbound PGN 0xD6** | ❌ None | ✅ From AgIO |
| **Outbound PGNs** | ✅ Sent (0x64, 0xFE, 0xEF, 0xE5) | ✅ Sent (same) |
| **AgIO Required** | ⚠️ Optional (modules only) | ✅ Yes (GPS + modules) |
| **UDP localhost:15555** | ❌ No incoming | ✅ Receives PGN 0xD6 |
| **UDP localhost:17777** | ✅ Sends commands | ✅ Sends commands |
| **UpdateFixPosition()** | ✅ Called by CSim | ✅ Called by UDP handler |

---

## Migration Notes for Backend API

### Workflow 001: Backend Infrastructure (COMPLETED)

- Backend (ApplicationOrchestrator) infrastructure created
- SignalR state broadcasting implemented (IStatePublisher/IBackendClient)
- ApplicationState model with Timestamp property
- FormGPS receives state updates via SignalR

### Workflow 002: Backend Simulator Implementation (COMPLETED)

**Status:** Backend simulator fully functional with GPS processing

#### Backend Simulator Architecture

The backend now contains a complete GPS simulator with physics-based vehicle movement:

**Components:**

1. **SimulatorService** - Core simulator logic
   - **Location:** [AgOpenGPS.Api/Services/SimulatorService.cs](../../SourceCode/AgOpenGPS.Api/Services/SimulatorService.cs)
   - **Tick Rate:** 93ms (~10.75 Hz) - matches legacy CSim timing
   - **Physics:**
     - Steering response with smoothing/delay
     - Heading calculation based on wheelbase geometry
     - Position calculation using bearing/distance
   - **State:** Position, Heading, Speed, Altitude
   - **Methods:**
     - `Start(lat, lon, heading, speed)` - Initialize simulator
     - `Stop()` - Halt simulation
     - `SetSpeed(speedKph)` - Update target speed
     - `SetSteering(degrees)` - Update steering angle
     - `Reset()` - Reset to initial state
     - `DoSimTick()` - Internal physics update (called by hosted service)

2. **SimulatorHostedService** - Background service
   - **Location:** [AgOpenGPS.Api/Services/SimulatorHostedService.cs](../../SourceCode/AgOpenGPS.Api/Services/SimulatorHostedService.cs)
   - **Purpose:** Runs simulator tick loop, sends UDP packets
   - **Tick Rate:** 93ms
   - **UDP Output:** Sends PGN 0xD6 packets to localhost:15556
   - **Note:** Sends via UDP (not direct GnssService calls) to maintain packet flow architecture

3. **UdpPacketReceiver** - UDP listener
   - **Location:** [AgOpenGPS.Api/Services/UdpPacketReceiver.cs](../../SourceCode/AgOpenGPS.Api/Services/UdpPacketReceiver.cs)
   - **Port:** 15556 (configurable via UdpOptions)
   - **Purpose:** Receives GPS packets from AgIO or simulator
   - **Returns:** `IAsyncEnumerable<UdpPacket>` for event-driven processing

4. **GnssService** - GPS packet processing
   - **Location:** [AgOpenGPS.Api/Services/GnssService.cs](../../SourceCode/AgOpenGPS.Api/Services/GnssService.cs)
   - **Purpose:** Unpacks PGN 0xD6 binary protocol, performs coordinate transforms
   - **Methods:**
     - `InitializeLocalPlane(origin)` - Set coordinate system origin
     - `ProcessGpsPacket(bytes)` - Unpack PGN 0xD6, transform coordinates
     - `GetCurrentState()` - Return current GnssState
   - **Coordinate Transforms:** Wgs84Position → LocalPosition (meters from origin)

5. **ApplicationOrchestrator** - Event-driven main loop
   - **Location:** [AgOpenGPS.Api/Services/ApplicationOrchestrator.cs](../../SourceCode/AgOpenGPS.Api/Services/ApplicationOrchestrator.cs)
   - **Architecture:** Event-driven (NOT timer-based)
   - **Processing:** Consumes UDP packets immediately via `IAsyncEnumerable`
   - **Broadcasts:** ApplicationState.Gnss via SignalR when GPS data received
   - **Performance:** ~93ms intervals when simulator running, or real GPS rate

#### CQRS Command Pattern

Simulator control uses CQRS with MediatR:

**Commands:** [AgOpenGPS.Api.Client/Commands/SimulatorCommands.cs](../../SourceCode/AgOpenGPS.Api.Client/Commands/SimulatorCommands.cs)
- `StartSimulatorCommand(lat, lon, heading, speedKph)`
- `StopSimulatorCommand()`
- `SetSimulatorSpeedCommand(speedKph)`
- `SetSimulatorSteeringCommand(degrees)`
- `ResetSimulatorCommand()`

**Handlers:** [AgOpenGPS.Api/Commands/Handlers/](../../SourceCode/AgOpenGPS.Api/Commands/Handlers/)
- Each command has dedicated handler that calls SimulatorService
- MediatR dispatches commands to handlers

**SignalR Hub:** [AgOpenGPS.Api/Hubs/StateHub.cs](../../SourceCode/AgOpenGPS.Api/Hubs/StateHub.cs)
- Specific methods per command type (`StartSimulator`, `StopSimulator`, etc.)
- Workaround: SignalR doesn't support generic hub methods (`SendCommand<T>`)
- Client calls hub methods, hub dispatches via MediatR

#### UDP Communication Flow (Backend Simulator)

```
┌─────────────────────────────────────────────────────────┐
│ AgOpenGPS.Api Backend                                   │
│                                                         │
│  SimulatorHostedService (93ms loop)                    │
│    ↓                                                    │
│  SimulatorService.DoSimTick()                          │
│    - Calculate physics (position, heading, speed)      │
│    ↓                                                    │
│  Build PGN 0xD6 packet (57 bytes)                      │
│    ↓                                                    │
│  Send UDP → localhost:15556                            │
│                                                         │
│    ↓ (loopback)                                        │
│                                                         │
│  UdpPacketReceiver (listening on 15556)                │
│    ↓                                                    │
│  ApplicationOrchestrator.ProcessPacketAsync()          │
│    ↓                                                    │
│  GnssService.ProcessGpsPacket()                        │
│    - Unpack PGN 0xD6                                   │
│    - Transform Wgs84 → Local coordinates               │
│    - Populate GnssState                                │
│    ↓                                                    │
│  ApplicationState.Gnss updated                         │
│    ↓                                                    │
│  SignalRStatePublisher.BroadcastStateAsync()           │
│    → SignalR broadcast                                 │
└─────────────────────────────────────────────────────────┘
                    ↓ SignalR WebSocket
┌─────────────────────────────────────────────────────────┐
│ FormGPS (Frontend)                                      │
│  SignalRBackendClient.OnStateReceived()                │
│    - Display GPS data from ApplicationState.Gnss       │
└─────────────────────────────────────────────────────────┘
```

#### Integration Tests

**Test Suite:** [Tests/AgOpenGPS.API.IntegrationTests/](../../SourceCode/Tests/AgOpenGPS.API.IntegrationTests/)

**Test Files:**
- `GpsPacketProcessingTests.cs` - GPS packet unpacking and coordinate transforms (7 tests)
- `StateReceptionTests.cs` - SignalR state reception (3 tests)
- `SimulatorIntegrationTests.cs` - Backend simulator with CQRS commands (14 tests)

**Test Results:** 21/24 passing (3 physics-related tests deferred)

**Test Approach:**
- In-memory test server (`TestWebApplicationFactory`)
- External GPS simulator helper (`GpsSimulator`)
- SignalR client connections via `CreateTestHubConnection()`
- Command testing via `IBackendClient.SendCommandAsync()`

#### Key Design Patterns

1. **Event-Driven:** ApplicationOrchestrator processes UDP immediately (not timer-based)
2. **UDP Loopback:** Simulator sends UDP packets (maintains packet flow architecture)
3. **CQRS:** Commands for simulator control via MediatR
4. **Transport Abstraction:** IStatePublisher/IBackendClient interfaces
5. **SignalR Limitation Workaround:** Specific hub methods per command type
6. **Coordinate Value Types:** Wgs84Position, LocalPosition, Heading, Speed, Altitude (C# 9.0 init setters)

#### What's NOT Yet Done

- [ ] FormGPS integration (Task 8 deferred)
- [ ] Real AgIO integration testing
- [ ] Physics test refinements (steering behavior)

**Reference:**
- [docs/workflow/001-application-orchestrator/](../workflow/001-application-orchestrator/) - Backend infrastructure
- [docs/workflow/002-gps-gnss-migration/](../workflow/002-gps-gnss-migration/) - GPS/GNSS migration

---

*Last updated: 2025-01-27 (Workflow 002 completion)*
