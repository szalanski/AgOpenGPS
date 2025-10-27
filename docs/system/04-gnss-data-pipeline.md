# GNSS Data Pipeline

## GPS Data Flow

External GPS receivers (via AgIO) send position data to the backend through a multi-stage pipeline:

```
External GPS → AgIO → UDP Packet → Backend Receiver → Unpacking →
Coordinate Transforms → State Population → Broadcasting
```

Each stage has specific responsibilities and handles different aspects of GPS data processing.

## Stage 1: External GPS Reception (AgIO)

**AgIO** is a separate program that interfaces with GPS hardware:

### AgIO Responsibilities
- **Hardware interface**: Connects to GPS receivers (serial, USB, Ethernet)
- **NMEA parsing**: Converts NMEA sentences to structured data
- **Protocol conversion**: Encodes GPS data into binary AgOpenGPS protocol
- **UDP transmission**: Sends binary packets to backend on port 15556

### Data Sources
- **RTK GPS**: High-precision (cm-level accuracy) via RTK corrections
- **Standard GPS**: Consumer GPS (meter-level accuracy)
- **GNSS receivers**: GPS, GLONASS, Galileo, BeiDou support
- **Virtual GPS**: Simulators or recorded data playback

**Note**: Backend doesn't communicate directly with GPS hardware. AgIO is the hardware abstraction layer.

## Stage 2: UDP Packet Reception

Backend receives binary GPS packets via UDP:

### UDP Listener
- **Port**: 15556 (configurable)
- **Protocol**: UDP (connectionless, low overhead)
- **Packet size**: 128 bytes (PGN 0xD6 format)
- **Rate**: Variable (5-10 Hz typical, depends on GPS)

### Why UDP?
- **Low latency**: No TCP handshake overhead
- **Simple**: No connection state management
- **Agriculture standard**: AgOpenGPS protocol uses UDP
- **Packet loss acceptable**: GPS sends redundant data (next packet corrects)

### Packet Format
Binary packet contains:
- **Header**: Packet type identifier (0xD6 for GPS)
- **Payload**: Position, heading, speed, altitude, quality metrics
- **Checksum**: Data integrity verification

## Stage 3: Binary Protocol Unpacking

Backend unpacks binary GPS data:

### PGN 0xD6 Protocol
128-byte binary format with fixed field positions:
- **Latitude**: 8 bytes (double, decimal degrees)
- **Longitude**: 8 bytes (double, decimal degrees)
- **Heading**: 2 bytes (unsigned short, degrees * 100)
- **Speed**: 2 bytes (unsigned short, km/h * 100)
- **Altitude**: 4 bytes (float, meters)
- **Fix quality**: 1 byte (0=invalid, 1=GPS, 2=DGPS, 4=RTK fix, 5=RTK float)
- **Satellite count**: 1 byte (number of satellites in view)
- **HDOP**: 2 bytes (horizontal dilution of precision * 100)
- **Age**: 2 bytes (age of correction data in seconds * 100)
- **Reserved**: Remaining bytes for future extensions

### Unpacking Process
1. **Validate header**: Confirm packet type is 0xD6
2. **Extract fields**: Read each field at fixed offset
3. **Convert units**: Scale integers back to real values (e.g., heading/100)
4. **Validate ranges**: Check latitude [-90, 90], longitude [-180, 180], etc.
5. **Build domain objects**: Create strongly-typed position, heading, speed objects

## Stage 4: Coordinate System Transformations

GPS uses **WGS84** global coordinates (latitude/longitude), but agriculture applications need **local plane** coordinates (X/Y meters) for field operations.

### Coordinate Systems

#### WGS84 (Global)
- **Latitude**: -90° to +90° (negative = south, positive = north)
- **Longitude**: -180° to +180° (negative = west, positive = east)
- **Altitude**: Meters above WGS84 ellipsoid
- **Use**: Global positioning, GPS receiver native format

#### Local Plane (Field)
- **Origin**: User-defined field reference point (typically field entrance)
- **X-axis**: East-West (meters, positive = east)
- **Y-axis**: North-South (meters, positive = north)
- **Z-axis**: Vertical (meters, positive = up)
- **Use**: Field mapping, guidance calculations, display

### Transformation Process
1. **Initialize local plane**: Set origin (latitude/longitude) once at field start
2. **Compute offsets**: Calculate WGS84 distance from origin
3. **Project to plane**: Convert spherical coordinates to flat plane (meters)
4. **Apply rotations**: Rotate to field orientation if needed

### Why Two Systems?
- **WGS84**: Universal, GPS-native, works anywhere on Earth
- **Local plane**: Simple Cartesian geometry, intuitive for operators (e.g., "50 meters east")
- **Guidance**: Path planning uses local plane (straight lines in meters)
- **Display**: Field maps use local coordinates (easier visualization)

### Transformation Accuracy
- **Flat Earth approximation**: Valid for fields up to ~10 km
- **Error**: <1 cm for typical field sizes
- **Spherical corrections**: Not needed at farm scale

## Stage 5: GPS Quality Metrics

Backend extracts GPS quality indicators:

### Fix Quality
- **No Fix (0)**: GPS not working or no satellites
- **GPS Fix (1)**: Standard GPS (meter-level accuracy)
- **DGPS Fix (2)**: Differential GPS (decimeter accuracy)
- **RTK Float (5)**: RTK without full ambiguity resolution (cm-level)
- **RTK Fix (4)**: RTK with ambiguity resolution (cm-level, best quality)

### Satellite Count
- **Minimum**: 4 satellites required for 3D fix
- **Good**: 8+ satellites (redundancy improves accuracy)
- **Excellent**: 12+ satellites (optimal geometry)

### HDOP (Horizontal Dilution of Precision)
- **Meaning**: Geometric quality of satellite constellation
- **Scale**: Lower is better
- **Excellent**: <2 (precision farming)
- **Good**: 2-5 (general navigation)
- **Poor**: >5 (use with caution)

### Age of Correction Data
- **RTK/DGPS only**: How old is correction data (seconds)
- **Fresh**: <3 seconds (optimal)
- **Acceptable**: 3-10 seconds
- **Stale**: >10 seconds (accuracy degrades)

### Health Status
Backend computes overall GPS health:
- **Healthy**: RTK fix + low HDOP + fresh corrections + 8+ satellites
- **Good**: GPS fix + acceptable HDOP + sufficient satellites
- **Degraded**: Poor fix quality or geometry
- **Invalid**: No fix or stale data

## Stage 6: State Population

Backend constructs GPS state from processed data:

### State Components
- **Position**: WGS84 + Local coordinates (both systems preserved)
- **Heading**: Degrees (0-360, 0=north, clockwise)
- **Speed**: km/h (ground speed)
- **Altitude**: Meters above sea level
- **Quality**: Fix type, satellite count, HDOP, age
- **Health**: Overall GPS health indicator

### Timestamp
- **Backend timestamp**: When backend processed this packet (UTC)
- **Not GPS timestamp**: GPS time is separate (not currently used)

## Stage 7: Broadcasting

Processed GPS state is broadcast to all connected clients:
- **Via SignalR**: Real-time push to frontend applications
- **Rate**: Matches GPS arrival rate (5-10 Hz typical)
- **All data**: Complete GPS state (not filtered or summarized)

## Error Handling

### Invalid Packets
- **Checksum failure**: Packet dropped, wait for next
- **Invalid ranges**: Packet dropped (e.g., latitude = 999)
- **Unknown type**: Logged, not processed

### GPS Signal Loss
- **No packets**: Backend stops broadcasting updates
- **Clients keep last state**: Stale but valid data
- **Age increases**: Clients can detect stale data via timestamp

### Coordinate Transformation Errors
- **Uninitialized origin**: Use default origin (temporary)
- **Invalid coordinates**: Skip transformation, use WGS84 only
- **Log errors**: Record for diagnostics

## Performance Characteristics

### Latency
- **UDP reception**: <1ms
- **Unpacking**: <1ms
- **Transformation**: <1ms
- **Total pipeline**: <5ms (GPS packet → Broadcast)

### Throughput
- **10 Hz GPS**: 600 packets/minute
- **Processing time**: ~3ms per packet average
- **CPU usage**: <5% (single core)

### Accuracy
- **Coordinate transform**: <1cm error (typical fields)
- **No data loss**: All GPS packets processed
- **Timestamp precision**: Millisecond resolution

## Future Enhancements

### Multi-GPS Support
- **Dual GPS**: Two GPS receivers for heading determination
- **Antenna offset**: Corrections for vehicle-mounted GPS
- **GPS fusion**: Combine multiple sources for redundancy

### IMU Integration
- **Orientation**: Roll, pitch, yaw from IMU
- **GPS/IMU fusion**: Kalman filter for smoother positioning
- **High-rate IMU**: 50-100 Hz for fast vehicle response

### RTK Base Station
- **Backend RTK base**: Provide corrections to field vehicles
- **NTRIP server**: Stream corrections over network
- **Multi-rover**: Support multiple vehicles from one base

## Related Documentation

- **[02-main-processing-loop.md](02-main-processing-loop.md)** - How GPS pipeline fits in main loop
- **[05-network-communication.md](05-network-communication.md)** - UDP packet details
- **[09-domain-model.md](09-domain-model.md)** - GPS domain concepts
- **[06-simulator-capabilities.md](06-simulator-capabilities.md)** - Simulated GPS generation
- **[adr/001-event-driven-architecture.md](adr/001-event-driven-architecture.md)** - Why GPS-driven
