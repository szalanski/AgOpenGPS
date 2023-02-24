void udpNMEA()
{
    // When ethernet is not running, return directly. parsePacket() will block when we don't
    if (udp.isRunning)
    {
        int packetLength = udp.NMEA.parsePacket();

        if (packetLength > 0)
        {
            udp.NMEA.read(udp.GPS_packetBuffer, packetLength);
            for (int i = 0; i < packetLength; i++)
            {
                parser << udp.GPS_packetBuffer[i];
            }
        }
    }
    else
    {
        return;
    }
}
