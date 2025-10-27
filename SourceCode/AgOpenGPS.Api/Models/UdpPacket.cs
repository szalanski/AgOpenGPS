namespace AgOpenGPS.Api.Models;

/// <summary>
/// Represents a validated UDP packet from AgIO.
/// Contains packet data, type, and validation state.
///
/// AgIO Binary Protocol Format:
/// [0] = 0x80 (Header byte 1)
/// [1] = 0x81 (Header byte 2)
/// [2] = 0x7F (Source address - not validated)
/// [3] = PGN (Packet type: 0xD6=GPS, 0xD3=IMU, etc.)
/// [4] = Payload length (N bytes)
/// [5..4+N] = Payload data
/// [5+N] = Checksum (sum of bytes[2..4+N])
/// </summary>
public class UdpPacket
{
    // AgIO Protocol Constants
    private const byte HeaderByte1 = 0x80;
    private const byte HeaderByte2 = 0x81;
    private const int HeaderSize = 2;
    private const int MinimumPacketSize = 5; // Header(2) + Source(1) + PGN(1) + Length(1)
    private const int HeaderOffset = 0;
    private const int SourceAddressOffset = 2;
    private const int PacketTypeOffset = 3;
    private const int PayloadLengthOffset = 4;
    private const int PayloadDataOffset = 5;

    public byte[] Data { get; }
    public int Length { get; }
    public UdpPacketType Type { get; }

    // PRIVATE constructor - force use of TryCreate factory
    private UdpPacket(byte[] data, int length, byte typeId)
    {
        Data = data;
        Length = length;
        Type = MapPacketType(typeId);
    }

    /// <summary>
    /// Create a validated UdpPacket from raw UDP bytes.
    /// Validates AgIO protocol structure: header, length, checksum.
    /// </summary>
    /// <param name="data">Raw UDP packet bytes from AgIO</param>
    /// <returns>Validated UdpPacket</returns>
    /// <exception cref="InvalidUdpPacketException">Thrown when packet validation fails</exception>
    public static UdpPacket Create(byte[] data)
    {
        // Step 1: Validate header bytes
        ValidateHeader(data);

        // Step 2: Validate packet length
        int packetLength = ValidateLength(data);

        // Step 3: Validate checksum
        ValidateChecksum(data, packetLength);

        // All validations passed - create packet
        byte packetTypeId = data[PacketTypeOffset];
        return new UdpPacket(data, packetLength, packetTypeId);
    }

    /// <summary>
    /// Validate AgIO packet header (must be 0x80 0x81).
    /// </summary>
    /// <exception cref="InvalidUdpPacketException">Thrown when header is invalid</exception>
    private static void ValidateHeader(byte[] data)
    {
        if (data.Length < MinimumPacketSize)
        {
            throw new InvalidUdpPacketException(
                $"Packet too short: {data.Length} bytes (minimum {MinimumPacketSize} bytes required)");
        }

        if (data[HeaderOffset] != HeaderByte1 || data[HeaderOffset + 1] != HeaderByte2)
        {
            throw new InvalidUdpPacketException(
                $"Invalid header: 0x{data[HeaderOffset]:X2} 0x{data[HeaderOffset + 1]:X2} " +
                $"(expected 0x{HeaderByte1:X2} 0x{HeaderByte2:X2})");
        }
    }

    /// <summary>
    /// Validate packet length against declared payload length.
    /// AgIO format: byte[4] contains payload length, total packet = 5 + payload length + 1 (checksum)
    /// </summary>
    /// <returns>Validated packet length (without checksum byte)</returns>
    /// <exception cref="InvalidUdpPacketException">Thrown when length is invalid</exception>
    private static int ValidateLength(byte[] data)
    {
        // Calculate expected packet length
        // Format: Header(2) + Source(1) + PGN(1) + Length(1) + Payload(N) + Checksum(1)
        int payloadLength = data[PayloadLengthOffset];
        int packetLength = PayloadDataOffset + payloadLength; // Total length without checksum
        int expectedTotalLength = packetLength + 1; // +1 for checksum byte

        if (data.Length < expectedTotalLength)
        {
            throw new InvalidUdpPacketException(
                $"Packet too short: expected {expectedTotalLength} bytes " +
                $"(payload={payloadLength}, got {data.Length} bytes)");
        }

        return packetLength;
    }

    /// <summary>
    /// Validate checksum (sum of bytes from source address to end of payload).
    /// Checksum = sum(bytes[2..packetLength]) stored at data[packetLength]
    /// </summary>
    /// <exception cref="InvalidUdpPacketException">Thrown when checksum is invalid</exception>
    private static void ValidateChecksum(byte[] data, int packetLength)
    {
        // Calculate checksum: sum bytes from source address (byte[2]) to end of payload
        byte calculatedChecksum = 0;
        for (int i = SourceAddressOffset; i < packetLength; i++)
        {
            calculatedChecksum += data[i];
        }

        byte receivedChecksum = data[packetLength];

        if (calculatedChecksum != receivedChecksum)
        {
            throw new InvalidUdpPacketException(
                $"Checksum mismatch: calculated 0x{calculatedChecksum:X2}, " +
                $"received 0x{receivedChecksum:X2}");
        }
    }

    /// <summary>
    /// Map AgIO packet type ID (PGN) to enum.
    /// </summary>
    private static UdpPacketType MapPacketType(byte typeId)
    {
        return typeId switch
        {
            0xD6 => UdpPacketType.Gps,
            0xD3 => UdpPacketType.Imu,
            0xD4 => UdpPacketType.Disconnect,
            253 => UdpPacketType.AutoSteer,
            250 => UdpPacketType.SensorData,
            221 => UdpPacketType.HardwareMessage,
            222 => UdpPacketType.Command,
            234 => UdpPacketType.RemoteSwitch,
            _ => UdpPacketType.Unknown
        };
    }
}

/// <summary>
/// UDP packet types from AgIO protocol (PGN values).
/// </summary>
public enum UdpPacketType
{
    Gps,              // 0xD6 - GPS position data
    Imu,              // 0xD3 - IMU data (heading, roll)
    Disconnect,       // 0xD4 - Disconnect notification
    AutoSteer,        // 253 - AutoSteer module feedback
    SensorData,       // 250 - Sensor data
    HardwareMessage,  // 221 - Hardware messages
    Command,          // 222 - Commands
    RemoteSwitch,     // 234 - Remote switch
    Unknown           // Other packet types
}
