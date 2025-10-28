# GNSS Capabilities

## Mission and Constraints

The GNSS subsystem converts raw satellite telemetry into navigation state for guidance, coverage, and steering modules. It must:

- Maintain centimetre-level accuracy when RTK-quality data is present.
- Surface signal health so operators and automation can make safe decisions.
- Provide both global (WGS84) and local (field-aligned) coordinate frames.
- Cope with lossy transport and occasional data gaps without corrupting downstream consumers.

## Inputs and Trust Model

- **Primary input** - AgIO PGN 0xD6 packets containing GNSS observations. Packets arrive over UDP and are considered untrusted until header, length, checksum, and sentinel checks pass.
- **Configuration input** - A field-origin WGS84 coordinate defining the local plane. A placeholder origin keeps mathematics consistent until a real value is provided.
- **Derived inputs** - Simulator packets reuse the same format so the system can rehearse end-to-end behaviour without hardware.

## Domain Model

- **Positioning**
  - `Wgs84Position` - global latitude/longitude and altitude, used for world-referenced features (boundaries, base stations).
  - `LocalPosition` - planar X/Y offsets from the field origin, preferred for guidance lines and rendering. Calculated with the `CoordinateTransformer`.
- **Orientation and motion**
  - `Heading` - dual-antenna heading when available, otherwise course-over-ground. Handles wrap-around and normalization.
  - `Speed` and `Altitude` - consistent units (km/h and metres) that treat sentinel values as "unknown".
- **Signal quality**
  - `GpsQuality` - fix type, satellite count, horizontal dilution of precision (HDOP), and correction age.
  - `GpsHealth` - packet frequency and watchdog counter, enabling drop detection and failover heuristics.
- **State aggregate**
  - `GnssState` - the canonical snapshot emitted to clients; consumers rely on this structure instead of parsing packets.

## Transformations and Invariants

1. **Validation guard** - Sentinel values (`double.MaxValue`, `float.MaxValue`) convert to nullable domain fields so consumers never encounter raw sentinels.
2. **Local plane establishment** - The backend withholds GNSS broadcasts until a plane origin is initialised, preserving downstream assumptions about coordinate availability.
3. **Frequency estimation** - A complementary filter balances responsiveness and stability, smoothing jitter but tracking sustained rate changes.
4. **Heading resolution** - Dual-antenna heading overrides single-antenna course; the fallback keeps behaviour deterministic across receiver types.
5. **Immutable snapshots** - Each `GnssState` is treated as immutable. Corrections create a new snapshot rather than mutating existing state, simplifying caching and replay.

## Error Handling Strategy

- Invalid packets are logged and dropped, with the watchdog counter incremented so consumers can apply their own timeout logic.
- If coordinate transformation cannot run (missing plane), health metrics still update but no public state is broadcast, preventing bogus positions from propagating.
- Recovery from data gaps prefers fresh data; clients interpret health metrics to decide how to degrade gracefully.

## Extensibility Roadmap

- **IMU fusion** - PGN 0xD3 packets already map to an enum value, enabling future blending of inertial data for low-speed accuracy.
- **Multiple origins** - Planned field management will permit switching between stored origins; the transformer will grow to track per-field metadata.
- **Quality alarms** - Threshold-based events (for example HDOP > 2.0 or age > 2.0 s) can publish warnings through the same state channel without altering packet formats.
