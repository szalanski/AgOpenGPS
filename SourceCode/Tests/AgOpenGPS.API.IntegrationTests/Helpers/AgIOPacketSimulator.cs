namespace AgOpenGPS.API.IntegrationTests.Helpers;

/// <summary>
/// Simulates AgIO binary GPS packets (PGN 0xD6).
/// Constructs valid 57-byte packets matching the AgIO/NMEA.Designer.cs format.
/// </summary>
public static class AgIOPacketSimulator
{
    // AgIO Protocol Constants
    private const byte HeaderByte1 = 0x80;
    private const byte HeaderByte2 = 0x81;
    private const byte SourceByte = 0x7C;
    private const byte PacketTypePGN = 0xD6; // GPS data
    private const byte PayloadLength = 0x33; // 51 bytes payload (57 total - 6 header/footer)
    private const int PacketSize = 57;

    // Byte offsets in packet
    private const int LongitudeOffset = 5;
    private const int LatitudeOffset = 13;
    private const int HeadingDualOffset = 21;
    private const int HeadingSingleOffset = 25;
    private const int SpeedOffset = 29;
    private const int RollOffset = 33;
    private const int AltitudeOffset = 37;
    private const int SatellitesOffset = 41;
    private const int FixQualityOffset = 43;
    private const int HdopOffset = 44;
    private const int AgeOffset = 46;
    private const int ImuHeadingOffset = 48;
    private const int ImuRollOffset = 50;
    private const int ImuPitchOffset = 52;
    private const int ImuYawRateOffset = 54;
    private const int ChecksumOffset = 56;

    /// <summary>
    /// Creates a complete GPS packet with all fields populated.
    /// </summary>
    public static byte[] CreateValidGpsPacket(
        double latitude,
        double longitude,
        float? headingDual = null,
        float? headingSingle = null,
        float? speed = null,
        float? altitude = null,
        ushort? satellites = null,
        byte? fixQuality = null,
        double? hdop = null,
        double? age = null,
        float? roll = null)
    {
        byte[] packet = new byte[PacketSize];

        // Header
        packet[0] = HeaderByte1;
        packet[1] = HeaderByte2;
        packet[2] = SourceByte;
        packet[3] = PacketTypePGN;
        packet[4] = PayloadLength;

        // Position (required fields)
        Buffer.BlockCopy(BitConverter.GetBytes(longitude), 0, packet, LongitudeOffset, 8);
        Buffer.BlockCopy(BitConverter.GetBytes(latitude), 0, packet, LatitudeOffset, 8);

        // Heading dual (optional - use MaxValue for "no data")
        float headingDualValue = headingDual ?? float.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(headingDualValue), 0, packet, HeadingDualOffset, 4);

        // Heading single (optional - use MaxValue for "no data")
        float headingSingleValue = headingSingle ?? float.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(headingSingleValue), 0, packet, HeadingSingleOffset, 4);

        // Speed (optional - use MaxValue for "no data")
        float speedValue = speed ?? float.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(speedValue), 0, packet, SpeedOffset, 4);

        // Roll (optional - use MaxValue for "no data")
        float rollValue = roll ?? float.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(rollValue), 0, packet, RollOffset, 4);

        // Altitude (optional - use MaxValue for "no data")
        float altitudeValue = altitude ?? float.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(altitudeValue), 0, packet, AltitudeOffset, 4);

        // Satellites tracked (optional - use MaxValue for "no data")
        ushort satellitesValue = satellites ?? ushort.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(satellitesValue), 0, packet, SatellitesOffset, 2);

        // Fix quality (optional - use MaxValue for "no data")
        packet[FixQualityOffset] = fixQuality ?? byte.MaxValue;

        // HDOP scaled by 100 (optional - use MaxValue for "no data")
        ushort hdopValue = hdop.HasValue ? (ushort)(hdop.Value * 100) : ushort.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(hdopValue), 0, packet, HdopOffset, 2);

        // Age scaled by 100 (optional - use MaxValue for "no data")
        ushort ageValue = age.HasValue ? (ushort)(age.Value * 100) : ushort.MaxValue;
        Buffer.BlockCopy(BitConverter.GetBytes(ageValue), 0, packet, AgeOffset, 2);

        // IMU data (not used in Task 6, set to MaxValue)
        Buffer.BlockCopy(BitConverter.GetBytes(ushort.MaxValue), 0, packet, ImuHeadingOffset, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(short.MaxValue), 0, packet, ImuRollOffset, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(short.MaxValue), 0, packet, ImuPitchOffset, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(short.MaxValue), 0, packet, ImuYawRateOffset, 2);

        // Calculate and add checksum (sum of bytes 2-56)
        int checksum = 0;
        for (int i = 2; i < ChecksumOffset; i++)
        {
            checksum += packet[i];
        }
        packet[ChecksumOffset] = (byte)checksum;

        return packet;
    }

    /// <summary>
    /// Creates a minimal GPS packet with only position and heading.
    /// Useful for basic connectivity tests.
    /// </summary>
    public static byte[] CreateMinimalGpsPacket(double latitude, double longitude, float heading)
    {
        return CreateValidGpsPacket(
            latitude: latitude,
            longitude: longitude,
            headingSingle: heading,
            fixQuality: 1 // GPS fix
        );
    }

    /// <summary>
    /// Creates a GPS packet with RTK Fixed quality and full metadata.
    /// Simulates high-quality GPS with all fields populated.
    /// </summary>
    public static byte[] CreateRtkFixedPacket(
        double latitude,
        double longitude,
        float heading,
        float speed,
        float altitude)
    {
        return CreateValidGpsPacket(
            latitude: latitude,
            longitude: longitude,
            headingDual: heading, // Dual antenna available in RTK
            headingSingle: heading,
            speed: speed,
            altitude: altitude,
            satellites: 16,
            fixQuality: 4, // RTK Fixed
            hdop: 0.8,
            age: 0.1
        );
    }

    /// <summary>
    /// Creates a GPS packet with no fix (all optional fields set to MaxValue).
    /// Simulates GPS searching for satellites.
    /// </summary>
    public static byte[] CreateNoFixPacket(double latitude, double longitude)
    {
        return CreateValidGpsPacket(
            latitude: latitude,
            longitude: longitude,
            fixQuality: 0 // No fix
        );
    }

    /// <summary>
    /// Creates a GPS packet with autonomous GPS fix (not RTK).
    /// </summary>
    public static byte[] CreateAutonomousGpsPacket(
        double latitude,
        double longitude,
        float heading,
        float speed)
    {
        return CreateValidGpsPacket(
            latitude: latitude,
            longitude: longitude,
            headingSingle: heading,
            speed: speed,
            altitude: 300.0f,
            satellites: 8,
            fixQuality: 1, // GPS autonomous
            hdop: 1.5,
            age: 1.0
        );
    }
}
