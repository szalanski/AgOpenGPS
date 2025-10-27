using System;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// GPS simulator service. Generates simulated GPS data for testing.
    /// Based on FormGPS CSim.DoSimTick() logic.
    /// </summary>
    public class SimulatorService
    {
        private const double DEG_TO_RAD = Math.PI / 180.0;
        private const double RAD_TO_DEG = 180.0 / Math.PI;
        private const double EARTH_RADIUS_KM = 6371.0;
        private const double TWO_PI = 2.0 * Math.PI;

        // Thread synchronization
        private readonly object _lock = new object();

        // Simulator state
        private double _latitude;
        private double _longitude;
        private double _headingRad;          // Current heading in radians
        private double _speedKmh;            // Current speed km/h
        private double _steerAngle;          // Target steering angle
        private double _steerAngleAve;       // Smoothed steering angle
        private double _stepDistance;        // Distance per tick

        // Initial position for Reset() (legacy behavior)
        private double _initialLatitude = 45.0;
        private double _initialLongitude = -93.0;

        // Smooth speed transition state
        private double _targetSpeed;         // Target speed for smooth transitions
        private const double ACCELERATION_RATE = 0.86;  // km/h per tick (matches legacy)
        private const double DECELERATION_RATE = 0.43;  // km/h per tick (matches legacy)

        public bool IsEnabled { get; private set; }

        public SimulatorService()
        {
            IsEnabled = false;
            _latitude = 45.0;
            _longitude = -93.0;
            _headingRad = 0.0;
            _speedKmh = 10.0;
            _targetSpeed = 10.0;
            _steerAngle = 0.0;
            _steerAngleAve = 0.0;
            _stepDistance = 0.0;
        }

        public void Start(double lat, double lon, double headingDeg, double speedKmh)
        {
            lock (_lock)
            {
                _latitude = lat;
                _longitude = lon;
                _initialLatitude = lat;    // Save for Reset() - legacy behavior
                _initialLongitude = lon;   // Save for Reset() - legacy behavior
                _headingRad = headingDeg * DEG_TO_RAD;
                _speedKmh = speedKmh;
                _targetSpeed = speedKmh;
                _steerAngle = 0.0;
                _steerAngleAve = 0.0;
                _stepDistance = 0.0;
                IsEnabled = true;
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                IsEnabled = false;
            }
        }

        public void SetSpeed(double speedKmh, bool smooth = false)
        {
            lock (_lock)
            {
                double clampedSpeed = Math.Clamp(speedKmh, -21.0, 322.0);

                if (smooth)
                {
                    // Set target for gradual transition
                    _targetSpeed = clampedSpeed;
                }
                else
                {
                    // Instant change
                    _speedKmh = clampedSpeed;
                    _targetSpeed = clampedSpeed;
                }
            }
        }

        public void AdjustSpeed(double delta)
        {
            lock (_lock)
            {
                _targetSpeed = Math.Clamp(_targetSpeed + delta, -21.0, 322.0);
            }
        }

        public void SetSpeedToZero()
        {
            lock (_lock)
            {
                _speedKmh = 0.0;
                _targetSpeed = 0.0;
            }
        }

        public void SetSteering(double steerAngle)
        {
            lock (_lock)
            {
                _steerAngle = steerAngle;
            }
        }

        public void ResetSteering()
        {
            lock (_lock)
            {
                _steerAngle = 0.0;
                _steerAngleAve = 0.0;
            }
        }

        public void ReverseDirection()
        {
            lock (_lock)
            {
                _headingRad += Math.PI;
                if (_headingRad > TWO_PI) _headingRad -= TWO_PI;
            }
        }

        public void ResetPosition(double lat, double lon)
        {
            lock (_lock)
            {
                _latitude = lat;
                _longitude = lon;
            }
        }

        /// <summary>
        /// Reset simulator to initial start position (legacy FormGPS behavior).
        /// Does NOT clear speed, steering, or stop simulator - only resets position.
        /// Matches: btnResetSim_Click in FormGPS (Controls.Designer.cs:2157)
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _latitude = _initialLatitude;
                _longitude = _initialLongitude;
                // Legacy behavior: Does NOT clear speed, steering, or disable simulator
            }
        }

        /// <summary>
        /// Simulation tick - updates position and generates GPS packet.
        /// Called by SimulatorHostedService timer (93ms).
        /// Returns PGN 0xD6 binary packet, or null if simulator disabled.
        /// </summary>
        public byte[]? Tick()
        {
            lock (_lock)
            {
                if (!IsEnabled)
                    return null;

                // Apply smooth speed transition (NEW)
                if (Math.Abs(_speedKmh - _targetSpeed) > 0.01)
                {
                    double rate = (_speedKmh < _targetSpeed) ? ACCELERATION_RATE : DECELERATION_RATE;
                    double speedDiff = _targetSpeed - _speedKmh;

                    if (Math.Abs(speedDiff) < rate)
                        _speedKmh = _targetSpeed;
                    else
                        _speedKmh += Math.Sign(speedDiff) * rate;
                }

                // Smooth steering angle (from CSim.DoSimTick)
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

                // Calculate step distance from speed (93ms tick, speed in km/h)
                _stepDistance = (_speedKmh / 3600.0) * 0.093; // 93ms = 0.093 seconds

                // Update heading based on steering (from CSim logic)
                double headingChange = _stepDistance * Math.Tan(_steerAngleAve * DEG_TO_RAD) / 2.0;
                _headingRad += headingChange;

                // Normalize heading to [0, 2π)
                if (_headingRad > TWO_PI) _headingRad -= TWO_PI;
                if (_headingRad < 0) _headingRad += TWO_PI;

                // Update position using great circle navigation
                (_latitude, _longitude) = CalculateNewPosition(_latitude, _longitude, _headingRad, _stepDistance);

                // Generate PGN 0xD6 packet
                return GenerateGpsPacket();
            }
        }

        /// <summary>
        /// Great circle navigation - calculates new position from bearing and distance.
        /// </summary>
        private (double lat, double lon) CalculateNewPosition(double latDeg, double lonDeg, double bearingRad, double distanceKm)
        {
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

        /// <summary>
        /// Simulate altitude based on lat/lon (from CSim.SimulateAltitude).
        /// </summary>
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

        /// <summary>
        /// Generate PGN 0xD6 binary GPS packet (AgIO format).
        /// 57 bytes: header + GPS data + checksum
        /// </summary>
        private byte[] GenerateGpsPacket()
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
            Buffer.BlockCopy(BitConverter.GetBytes(_longitude), 0, packet, 5, 8);

            // Latitude (bytes 13-20)
            Buffer.BlockCopy(BitConverter.GetBytes(_latitude), 0, packet, 13, 8);

            // Heading dual antenna (bytes 21-24) - not used in single antenna sim
            Buffer.BlockCopy(BitConverter.GetBytes(float.MaxValue), 0, packet, 21, 4);

            // Heading true (bytes 25-28)
            Buffer.BlockCopy(BitConverter.GetBytes((float)(_headingRad * RAD_TO_DEG)), 0, packet, 25, 4);

            // Speed (bytes 29-32)
            Buffer.BlockCopy(BitConverter.GetBytes((float)_speedKmh), 0, packet, 29, 4);

            // Roll (bytes 33-36) - not used
            Buffer.BlockCopy(BitConverter.GetBytes(float.MaxValue), 0, packet, 33, 4);

            // Altitude (bytes 37-40)
            Buffer.BlockCopy(BitConverter.GetBytes((float)SimulateAltitude(_latitude, _longitude)), 0, packet, 37, 4);

            // Satellites tracked (bytes 41-42)
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)12), 0, packet, 41, 2);

            // Fix quality (byte 43)
            packet[43] = 4; // RTK Fixed

            // HDOP (bytes 44-45) - 0.7 * 100
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)70), 0, packet, 44, 2);

            // Age (bytes 46-47) - 0.1 * 100
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)10), 0, packet, 46, 2);

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
