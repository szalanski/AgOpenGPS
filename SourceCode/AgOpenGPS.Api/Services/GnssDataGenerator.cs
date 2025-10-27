using System;

namespace AgOpenGPS.Api.Services
{
    /// <summary>
    /// GNSS/GPS data generation for simulator.
    /// Generates simulated GPS-specific data: altitude, satellite count, fix quality, HDOP, age.
    /// </summary>
    public class GnssDataGenerator
    {
        /// <summary>
        /// Simulate altitude based on lat/lon (from CSim.SimulateAltitude).
        /// Creates pseudo-random but deterministic altitude based on position.
        /// </summary>
        public double SimulateAltitude(double latitude, double longitude)
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
        /// Get simulated satellite count (always 12 for simulator - good GPS constellation).
        /// </summary>
        public ushort GetSatelliteCount()
        {
            return 12;
        }

        /// <summary>
        /// Get simulated fix quality (4 = RTK Fixed - best quality).
        /// </summary>
        public byte GetFixQuality()
        {
            return 4; // RTK Fixed
        }

        /// <summary>
        /// Get simulated HDOP (Horizontal Dilution of Precision).
        /// Returns 70 (0.7 * 100) - excellent precision.
        /// </summary>
        public ushort GetHdop()
        {
            return 70; // 0.7 * 100
        }

        /// <summary>
        /// Get simulated age of differential correction.
        /// Returns 10 (0.1 * 100) - very fresh data.
        /// </summary>
        public ushort GetAge()
        {
            return 10; // 0.1 * 100
        }
    }
}
