using System;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// AgIO protocol serialization for GPS packets.
    /// Encodes GPS data into binary PGN 0xD6 packets for AgIO communication.
    /// </summary>
    public class AgIoProtocolSerializer
    {
        /// <summary>
        /// Encodes GPS data into a binary packet following the AgIO protocol format (PGN 0xD6).
        /// Returns a 57-byte packet: header + GPS data + checksum.
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
        /// <param name="headingDeg">Heading in degrees</param>
        /// <param name="speedKmh">Speed in km/h</param>
        /// <param name="altitude">Altitude in meters</param>
        /// <param name="satelliteCount">Number of satellites tracked</param>
        /// <param name="fixQuality">Fix quality (4 = RTK Fixed)</param>
        /// <param name="hdop">HDOP * 100</param>
        /// <param name="age">Age * 100</param>
        /// <returns>57-byte binary packet</returns>
        public byte[] EncodeGpsDataPacket(
            double latitude,
            double longitude,
            double headingDeg,
            double speedKmh,
            double altitude,
            ushort satelliteCount,
            byte fixQuality,
            ushort hdop,
            ushort age)
        {
            byte[] packet = new byte[57];

            // Header: 0x80 0x81 0x7F 0xD6
            packet[0] = 0x80;
            packet[1] = 0x81;
            packet[2] = 0x7F;
            packet[3] = 0xD6;

            // Payload length
            packet[4] = 51; // 57 - 6 (header + length + checksum)

            // Longitude (bytes 5-12)
            Buffer.BlockCopy(BitConverter.GetBytes(longitude), 0, packet, 5, 8);

            // Latitude (bytes 13-20)
            Buffer.BlockCopy(BitConverter.GetBytes(latitude), 0, packet, 13, 8);

            // Heading dual antenna (bytes 21-24) - not used in single antenna sim
            Buffer.BlockCopy(BitConverter.GetBytes(float.MaxValue), 0, packet, 21, 4);

            // Heading true (bytes 25-28)
            Buffer.BlockCopy(BitConverter.GetBytes((float)headingDeg), 0, packet, 25, 4);

            // Speed (bytes 29-32)
            Buffer.BlockCopy(BitConverter.GetBytes((float)speedKmh), 0, packet, 29, 4);

            // Roll (bytes 33-36) - not used
            Buffer.BlockCopy(BitConverter.GetBytes(float.MaxValue), 0, packet, 33, 4);

            // Altitude (bytes 37-40)
            Buffer.BlockCopy(BitConverter.GetBytes((float)altitude), 0, packet, 37, 4);

            // Satellites tracked (bytes 41-42)
            Buffer.BlockCopy(BitConverter.GetBytes(satelliteCount), 0, packet, 41, 2);

            // Fix quality (byte 43)
            packet[43] = fixQuality;

            // HDOP (bytes 44-45)
            Buffer.BlockCopy(BitConverter.GetBytes(hdop), 0, packet, 44, 2);

            // Age (bytes 46-47)
            Buffer.BlockCopy(BitConverter.GetBytes(age), 0, packet, 46, 2);

            // IMU heading (bytes 48-49) - not used
            Buffer.BlockCopy(BitConverter.GetBytes(ushort.MaxValue), 0, packet, 48, 2);

            // IMU roll (bytes 50-51) - not used
            Buffer.BlockCopy(BitConverter.GetBytes(short.MaxValue), 0, packet, 50, 2);

            // IMU pitch (bytes 52-53) - not used
            Buffer.BlockCopy(BitConverter.GetBytes(short.MaxValue), 0, packet, 52, 2);

            // IMU yaw rate (bytes 54-55) - not used
            Buffer.BlockCopy(BitConverter.GetBytes(short.MaxValue), 0, packet, 54, 2);

            // Calculate checksum (byte 56)
            byte checksum = 0;
            for (int i = 2; i < 56; i++)
            {
                checksum += packet[i];
            }
            packet[56] = checksum;

            return packet;
        }
    }
}
