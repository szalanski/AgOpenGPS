---
date: 2025-11-12T09:00:00-08:00
researcher: Claude
git_commit: e6d72a4ef1d3209ddfc35dd84aca761aaa5ff291
branch: cross-platform-support
repository: AgOpenGPS
topic: "Comprehensive UDP Protocol Analysis: AgIO and AgOpenGPS Communication"
codebase_type: mixed
directories: SourceCode/GPS, SourceCode/AgIO, SourceCode/AgOpenGPS.Api, SourceCode/ModSim, SourceCode/AgDiag
tags: research, codebase, udp-protocol, agio, communication, pgn, nmea
status: complete
last_updated: 2025-11-12
last_updated_by: Claude
---

# Research: Comprehensive UDP Protocol Analysis - AgOpenGPS and AgIO Communication

**Date**: 2025-11-12T09:00:00-08:00
**Researcher**: Claude
**Git Commit**: e6d72a4ef1d3209ddfc35dd84aca761aaa5ff291
**Branch**: cross-platform-support
**Repository**: AgOpenGPS

## Research Question

Analyze AgIO and FormGPS.cs along with related files to create a comprehensive UDP protocol description for the AgOpenGPS communication system.

## Executive Summary

The AgOpenGPS ecosystem uses a custom binary protocol based on PGN (Parameter Group Number) messages for all communication between components. AgIO acts as the central communication hub, bridging between the main application (FormGPS), external GPS hardware, and Arduino-based control modules (steering, machine control, relay). The protocol operates over UDP sockets using both loopback (localhost) and network broadcast mechanisms, with a consistent packet structure featuring fixed headers and checksums. The system is currently undergoing migration to a backend API architecture using the Strangler Fig pattern, where new code paths run parallel to legacy implementations.

## Architecture Overview

### Communication Topology

```
┌─────────────┐         UDP:15555          ┌──────────────┐       UDP:9999        ┌──────────────┐
│   FormGPS   │◄───────────────────────────│     AgIO     │◄────────────────────────│   Modules    │
│  (Main App) │                            │ (Comm Hub)   │                         │  (Hardware)  │
└─────────────┘         UDP:17777          └──────────────┘       UDP:8888         └──────────────┘
       │       ─────────────────────────────►      │        ─────────────────────────►      │
       │                                           │                                         │
       │           ┌────────────────┐             │                Serial/USB               │
       └──────────►│  Backend API   │             └─────────────────────────────────────────┘
                   │   (New/WIP)    │
                   └────────────────┘
                      UDP:15556
```

### Port Allocation

| Port | Direction | Purpose | Protocol |
|------|-----------|---------|----------|
| **15555** | AgIO → FormGPS | GPS data, module responses | UDP Loopback |
| **15556** | FormGPS → Backend API | GPS data forwarding (migration) | UDP Loopback |
| **17777** | FormGPS → AgIO | Control commands, settings | UDP Loopback |
| **8888** | AgIO ↔ Modules | Hardware module communication | UDP Broadcast |
| **9999** | Modules → AgIO | Network interface | UDP |

## Binary Protocol Structure

### Universal Packet Format

All packets follow this structure:

```
┌──────────┬──────────┬──────────┬──────────┬──────────┬─────────────┬──────────┐
│ Byte 0   │ Byte 1   │ Byte 2   │ Byte 3   │ Byte 4   │ Bytes 5..n  │ Byte n+1 │
├──────────┼──────────┼──────────┼──────────┼──────────┼─────────────┼──────────┤
│   0x80   │   0x81   │  Source  │   PGN    │  Length  │   Payload   │ Checksum │
└──────────┴──────────┴──────────┴──────────┴──────────┴─────────────┴──────────┘
```

**Field Descriptions:**
- **Bytes 0-1**: Fixed header (0x80, 0x81) - identifies AgIO protocol
- **Byte 2**: Source address (0x7F = FormGPS, 0x7C = AgIO, 0x7B = Module)
- **Byte 3**: PGN (Parameter Group Number) - identifies message type
- **Byte 4**: Payload length in bytes
- **Bytes 5..n**: Variable-length payload data
- **Byte n+1**: Checksum (sum of bytes 2 through n)

### Checksum Calculation

```csharp
// From UDPComm.Designer.cs:392-397
int crc = 0;
for (int i = 2; i + 1 < byteData.Length; i++)
{
    crc += byteData[i];
}
byteData[byteData.Length - 1] = (byte)crc;
```

## PGN Message Catalog

### GPS and Navigation

#### PGN 0xD6 (214) - GPS Position Data
**Size**: 57 bytes | **Direction**: AgIO → FormGPS | **Frequency**: 4-10 Hz

```
Offset | Size | Type    | Field                  | Notes
-------|------|---------|------------------------|------------------------
5-12   | 8    | double  | Longitude (degrees)    | MaxValue = unavailable
13-20  | 8    | double  | Latitude (degrees)     | MaxValue = unavailable
21-24  | 4    | float   | Heading Dual Antenna   | MaxValue = unavailable
25-28  | 4    | float   | Heading Single         | From VTG sentence
29-32  | 4    | float   | Speed (km/h)           | From VTG sentence
33-36  | 4    | float   | Roll (degrees)         | From dual antenna
37-40  | 4    | float   | Altitude (meters)      | From GGA sentence
41-42  | 2    | ushort  | Satellites Tracked     | MaxValue = unavailable
43     | 1    | byte    | Fix Quality            | 0=None, 1=GPS, 4=RTK Fix
44-45  | 2    | ushort  | HDOP * 100             | Horizontal dilution
46-47  | 2    | ushort  | Age * 100 (seconds)    | Differential age
48-49  | 2    | ushort  | IMU Heading * 10       | MaxValue = unavailable
50-51  | 2    | short   | IMU Roll * 10          | MaxValue = unavailable
52-53  | 2    | short   | IMU Pitch              | MaxValue = unavailable
54-55  | 2    | short   | IMU Yaw Rate           | MaxValue = unavailable
```

#### PGN 0xD3 (211) - External IMU Data
**Size**: 14 bytes | **Direction**: IMU Module → AgIO → FormGPS

```
Offset | Size | Type  | Field              | Notes
-------|------|-------|-------------------|----------------------
5-6    | 2    | Int16 | Heading * 10      | Degrees
7-8    | 2    | Int16 | Roll * 10         | Degrees, with filtering
9-10   | 2    | Int16 | Angular Velocity  | Degrees/sec * -2
```

#### PGN 0xD4 (212) - IMU Disconnect
**Size**: 9 bytes | **Direction**: AgIO → FormGPS

Sets sentinel values: `imuHeading = 99999`, `imuRoll = 88888`

### Steering Control

#### PGN 0xFE (254) - AutoSteer Command
**Size**: 13 bytes | **Direction**: FormGPS → Modules | **Frequency**: 4-10 Hz

```
Offset | Size | Type  | Field              | Notes
-------|------|-------|-------------------|-------------------------
5-6    | 2    | Int16 | Speed * 10        | km/h
7      | 1    | byte  | Status            | 0=Pause, 1=Active
8-9    | 2    | Int16 | Steer Angle * 100 | Target angle in degrees
10     | 1    | byte  | Line Distance     | cm from guidance line
11     | 1    | byte  | Sections 1-8      | Bitmap
12     | 1    | byte  | Sections 9-16     | Bitmap
```

#### PGN 0xFD (253) - AutoSteer Response
**Size**: 14 bytes | **Direction**: Module → AgIO → FormGPS

```
Offset | Size | Type  | Field                | Notes
-------|------|-------|---------------------|------------------------
5-6    | 2    | Int16 | Actual Angle * 100  | From WAS sensor
7-8    | 2    | Int16 | Heading * 10        | 9999 = not available
9-10   | 2    | Int16 | Roll * 10           | 8888 = not available
11     | 1    | byte  | Switch Status       | Bit 0=work, Bit 1=steer
12     | 1    | byte  | PWM Value          | 0-255
```

#### PGN 0xFC (252) - Steer Settings
**Size**: 13 bytes | **Direction**: FormGPS → Module

```
Offset | Size | Type  | Field              | Notes
-------|------|-------|-------------------|------------------------
5      | 1    | byte  | Gain Proportional | PID Kp value
6      | 1    | byte  | High PWM Limit    | Maximum PWM
7      | 1    | byte  | Low PWM Limit     | Minimum for movement
8      | 1    | byte  | Min PWM           | Absolute minimum
9      | 1    | byte  | Counts Per Degree | WAS calibration
10-11  | 2    | Int16 | WAS Offset        | Wheel angle sensor zero
12     | 1    | byte  | Ackerman %        | Steering geometry
```

#### PGN 0xFB (251) - Steer Configuration
**Size**: 13 bytes | **Direction**: FormGPS → Module

```
Offset | Size | Type | Field        | Notes
-------|------|------|--------------|------------------------
5      | 1    | byte | Setting 0    | Configuration flags
6      | 1    | byte | Max Pulse    | Position control limit
7      | 1    | byte | Min Speed    | Speed * 10 (km/h)
8      | 1    | byte | Setting 1    | Additional config
9      | 1    | byte | Ang Velocity | 0=Off, 1=Constant contour
```

### Machine Control

#### PGN 0xEF (239) - Machine Data
**Size**: 13 bytes | **Direction**: FormGPS → Module | **Frequency**: 60 Hz

```
Offset | Size | Type | Field         | Notes
-------|------|------|---------------|------------------------
5      | 1    | byte | U-Turn State  | 0=Normal, 1=Turning
6      | 1    | byte | Speed         | Vehicle speed
7      | 1    | byte | Hydraulic     | 0=Off, 1=Lower, 2=Raise
8      | 1    | byte | Tram Lines    | Tram state
9      | 1    | byte | GeoStop       | 0=In bounds, 1=Out
11     | 1    | byte | Sections 1-8  | Section states bitmap
12     | 1    | byte | Sections 9-16 | Section states bitmap
```

#### PGN 0xEE (238) - Machine Configuration
**Size**: 13 bytes | **Direction**: FormGPS → Module

```
Offset | Size | Type | Field       | Notes
-------|------|------|-------------|------------------------
5      | 1    | byte | Raise Time  | Hydraulic (deciseconds)
6      | 1    | byte | Lower Time  | Hydraulic (deciseconds)
7      | 1    | byte | Enable Hyd  | 0=Off, 1=On
8      | 1    | byte | Setting 0   | Configuration flags
9-12   | 4    | byte | User 1-4    | User-defined values
```

#### PGN 0xEC (236) - Relay Configuration
**Size**: 30 bytes | **Direction**: FormGPS → Module

Contains 24 pin configuration bytes (offsets 5-28) defining relay/pin functions.

#### PGN 0xEB (235) - Section Dimensions
**Size**: 39 bytes | **Direction**: FormGPS → Module

Contains 16 section widths (2 bytes each) + section count:
- Offsets 5-36: Section widths (Int16, width * 100 in cm)
- Offset 37: Number of sections

#### PGN 0xE5 (229) - Section Control (Zones)
**Size**: 16 bytes | **Direction**: FormGPS → Module

Supports up to 64 sections using 8 bytes of bitmaps (offsets 5-12).

### System Messages

#### PGN 200 (0xC8) - Hello/Heartbeat
**Size**: 9 bytes | **Direction**: AgIO → Modules | **Frequency**: 0.5 Hz

Keepalive message: `{ 0x80, 0x81, 0x7F, 200, 3, 56, 0, 0, 0x47 }`

#### PGN 221 (0xDD) - Hardware Messages
**Size**: Variable | **Direction**: Module → FormGPS

```
Offset | Size | Type   | Field           | Notes
-------|------|--------|-----------------|------------------------
5      | 1    | byte   | Display Time    | Seconds
6      | 1    | byte   | Message Type    | 0=Error, 1=Info
7..n   | var  | string | Message Text    | UTF-8 encoded
```

#### PGN 222 (0xDE) - Remote Commands
**Size**: Variable | **Direction**: Module → FormGPS

```
Offset | Size | Type | Field        | Notes
-------|------|------|--------------|------------------------
5      | 1    | byte | Command Mask | Which commands are valid
6      | 1    | byte | Command Data | Command parameters
```

## NMEA Processing Pipeline

### AgIO NMEA Parsing Flow

1. **Serial Reception** ([SerialComm.Designer.cs:1019](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\SerialComm.Designer.cs#L1019))
   - Receives raw NMEA sentences from GPS hardware
   - Accumulates in `rawBuffer` string

2. **Sentence Parsing** ([NMEA.Designer.cs:91](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\NMEA.Designer.cs#L91))
   - Extracts sentences between `$` and `\r` markers
   - Validates checksum (XOR for standard NMEA)
   - Routes to specific parsers by sentence type

3. **Supported NMEA Sentences**:
   - **$GPGGA/$GNGGA**: Position, altitude, satellites, fix quality
   - **$GPVTG/$GNVTG**: Speed and heading
   - **$GPHDT/$GNHDT**: Dual antenna true heading
   - **$PAOGI**: AgOpenGPS custom format with IMU
   - **$PANDA**: Panda board format
   - **$GPHPD**: Hemisphere dual GPS heading + roll
   - **$PTNL,AVR**: Trimble dual antenna
   - **$GNTRA**: UBlox ub482 heading + roll
   - **$PSTI**: SkyTraq dual antenna baseline

4. **Binary Packet Assembly** ([NMEA.Designer.cs:233](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\NMEA.Designer.cs#L233))
   - Builds 57-byte PGN 0xD6 packet
   - Populates fields from parsed NMEA data
   - Uses sentinel values for unavailable fields

5. **UDP Transmission** ([UDP.designer.cs:156](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\UDP.designer.cs#L156))
   - Sends to FormGPS on 127.0.0.1:15555
   - Optional broadcast to module network

### Data Conversion Examples

#### Latitude/Longitude Conversion
```csharp
// NMEA format: DDMM.MMMM → Decimal degrees
// From NMEA.Designer.cs:427-442
double degreesLat = Math.Floor(Double.Parse(words[2]) * 0.01);
double minutesLat = Double.Parse(words[2]) - degreesLat * 100;
double DecDegLat = degreesLat + minutesLat * 0.016666666667;
if (words[3] == "S") DecDegLat *= -1;
```

#### Speed Conversion
```csharp
// Knots to km/h
speed_kmh = speed_knots * 1.852;

// km/h to knots
speed_knots = speed_kmh * 0.5399568;
```

#### Angle Encoding
```csharp
// Degrees to protocol format
encoded = (Int16)(degrees * 100);  // For steering angles
encoded = (Int16)(degrees * 10);   // For IMU heading/roll
```

## Network Communication

### UDP Socket Configuration

#### FormGPS Loopback ([UDPComm.Designer.cs:322](d:\Git\AgOpenGPS\SourceCode\GPS\Forms\UDPComm.Designer.cs#L322))
```csharp
loopBackSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
loopBackSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
loopBackSocket.Bind(new IPEndPoint(IPAddress.Loopback, 15555));
loopBackSocket.BeginReceiveFrom(loopBuffer, 0, loopBuffer.Length, SocketFlags.None,
    ref endPointLoopBack, new AsyncCallback(ReceiveAppData), null);
```

#### AgIO Network Server ([UDP.designer.cs:80](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\UDP.designer.cs#L80))
```csharp
UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
UDPSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
UDPSocket.Bind(new IPEndPoint(IPAddress.Any, 9999));
```

### Broadcast Configuration

- **Module Network**: 192.168.5.255:8888 (configurable subnet)
- **Global Broadcast**: 255.255.255.255:8888 (for discovery)
- **Loopback Broadcast**: 127.255.255.255 (FormGPS ↔ AgIO)

### Module Discovery Protocol

1. **Scan Packet**: `{ 0x80, 0x81, 0x7F, 202, 3, 202, 202, 5, 0x47 }`
2. **Response (PGN 203)**: Module IP in bytes 5-8, subnet in bytes 9-11
3. **IP Configuration**: `{ 0x80, 0x81, 0x7F, 201, 5, 201, 201, IP1, IP2, IP3, 0x47 }`

## AgIO Routing Logic

### PGN Routing Table ([UDP.designer.cs:196](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\UDP.designer.cs#L196))

| PGN | Hex | Description | Route To |
|-----|-----|-------------|----------|
| 254 | 0xFE | AutoSteer Data | Steer + Machine modules |
| 239 | 0xEF | Machine Data | Machine + Steer modules |
| 229 | 0xE5 | Section Control | Machine module only |
| 252 | 0xFC | Steer Settings | Steer module only |
| 251 | 0xFB | Steer Config | Steer module only |
| 238 | 0xEE | Machine Config | Machine + Steer modules |
| 236 | 0xEC | Relay Config | Machine + Steer modules |
| 235 | 0xEB | Section Dimensions | Machine module only |

### Serial Bridge

AgIO bridges UDP and serial for USB-connected modules:

1. **Serial → UDP**: Receives from COM port, validates packet, forwards to FormGPS
2. **UDP → Serial**: Receives from FormGPS, routes by PGN, writes to COM port
3. **Packet Validation**: Same format for both transports

## Backend Migration (Strangler Fig Pattern)

### Current Implementation

1. **GPS Packet Forwarding** ([UDPComm.Designer.cs:372](d:\Git\AgOpenGPS\SourceCode\GPS\Forms\UDPComm.Designer.cs#L372))
   - FormGPS receives PGN 0xD6 on port 15555
   - Forwards to backend API on port 15556
   - Backend processes through GnssService

2. **Backend Bypass** ([UDPComm.Designer.cs:60](d:\Git\AgOpenGPS\SourceCode\GPS\Forms\UDPComm.Designer.cs#L60))
   - When backend connected, skip legacy GPS processing
   - Backend sends state via SignalR
   - FormGPS uses backend state instead of legacy parsing

3. **State Synchronization** ([FormGPS.cs:590](d:\Git\AgOpenGPS\SourceCode\GPS\Forms\FormGPS.cs#L590))
   - Backend state received via `OnStateReceived()`
   - Updates local plane, camera heading, GPS position
   - Triggers `UpdateFixPosition()` for navigation

### Migration Status

- **Completed**: GPS data processing, simulator control
- **In Progress**: Module communication, section control
- **Pending**: Direct module communication from backend

## Sentinel Values

The protocol uses special values to indicate unavailable data:

| Type | Sentinel | Used For |
|------|----------|----------|
| `double.MaxValue` | 1.7976931348623157E+308 | Lat/Lon unavailable |
| `float.MaxValue` | 3.40282347E+38 | Heading/Speed/Roll unavailable |
| `ushort.MaxValue` | 65535 | Satellites/HDOP/Age unavailable |
| `short.MaxValue` | 32767 | IMU data unavailable |
| `99999` | - | IMU heading not available |
| `88888` | - | IMU roll not available |
| `9999` | - | Generic unavailable (heading) |
| `8888` | - | Generic unavailable (roll) |

## Error Handling

### Checksum Validation
- All packets validated on receive
- Invalid checksums silently discarded
- No retry mechanism (next cycle resends)

### Connection Monitoring
- Heartbeat counters track module connectivity
- Timeout after missing hello packets
- UI indicators show connection status

### Rate Limiting
- `udpWatchLimit = 70ms` prevents GPS flooding
- Ensures processing doesn't overwhelm CPU

## Key Implementation Files

### Core Protocol
- [PGN.Designer.cs](d:\Git\AgOpenGPS\SourceCode\GPS\Forms\PGN.Designer.cs) - PGN class definitions
- [UDPComm.Designer.cs](d:\Git\AgOpenGPS\SourceCode\GPS\Forms\UDPComm.Designer.cs) - FormGPS UDP implementation
- [UDP.designer.cs](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\UDP.designer.cs) - AgIO UDP server

### NMEA Processing
- [NMEA.Designer.cs](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\NMEA.Designer.cs) - NMEA parsing and packet building
- [SerialComm.Designer.cs](d:\Git\AgOpenGPS\SourceCode\AgIO\Source\Forms\SerialComm.Designer.cs) - Serial port handling

### Backend API
- [UdpPacket.cs](d:\Git\AgOpenGPS\SourceCode\AgOpenGPS.Api\Models\UdpPacket.cs) - Protocol validation
- [AgIoProtocolSerializer.cs](d:\Git\AgOpenGPS\SourceCode\AgOpenGPS.Api\Services\AgIoProtocolSerializer.cs) - Packet encoding
- [GnssService.cs](d:\Git\AgOpenGPS\SourceCode\AgOpenGPS.Api\Services\GnssService.cs) - GPS processing
- [UdpPacketReceiver.cs](d:\Git\AgOpenGPS\SourceCode\AgOpenGPS.Api\Services\UdpPacketReceiver.cs) - UDP reception

### Testing and Simulation
- [CSim.cs](d:\Git\AgOpenGPS\SourceCode\GPS\Classes\CSim.cs) - Legacy simulator
- [SimulatorHostedService.cs](d:\Git\AgOpenGPS\SourceCode\AgOpenGPS.Api\Services\SimulatorHostedService.cs) - Backend simulator
- [ModSim](d:\Git\AgOpenGPS\SourceCode\ModSim) - External module simulator

## Architecture Insights

1. **Hub-Spoke Architecture**: AgIO centralizes all communication, simplifying module implementation
2. **Protocol Consistency**: Same binary format for UDP and serial enables flexible hardware connectivity
3. **Broadcast Model**: Modules filter relevant PGNs, allowing flexible configuration
4. **Parallel Migration**: Strangler Fig pattern enables gradual backend adoption without breaking changes
5. **Sentinel Values**: Protocol gracefully handles optional/unavailable data
6. **Rate Management**: Multiple timing mechanisms prevent data flooding

## Open Questions

1. **Module Response Timing**: What are the latency requirements for module responses?
2. **Error Recovery**: Should the protocol implement acknowledgments or retries?
3. **Protocol Versioning**: How will protocol changes be managed during migration?
4. **Security**: Are there plans for authentication or encryption of UDP packets?
5. **Module Capabilities**: How do modules advertise their supported PGNs?

## Related Documentation

- [UDP Communication Architecture](d:\Git\AgOpenGPS\docs\architecture\06-udp-communication-and-simulator.md)
- [Backend Architecture](d:\Git\AgOpenGPS\docs\architecture\03-backend-driven.md)
- [Communication Protocols](d:\Git\AgOpenGPS\docs\implementation\sections\communication-protocols.md)