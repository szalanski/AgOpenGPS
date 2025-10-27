using System;

namespace AgOpenGPS.Api.Utilities
{
    /// <summary>
    /// Mathematical utility functions for coordinate transformations and geodetic calculations.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">Angle in degrees</param>
        /// <returns>Angle in radians</returns>
        public static double DegreesToRadians(double degrees)
        {
            const double DegreesToRadiansConversion = Math.PI / 180.0;
            return degrees * DegreesToRadiansConversion;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">Angle in radians</param>
        /// <returns>Angle in degrees</returns>
        public static double RadiansToDegrees(double radians)
        {
            const double RadiansToDegreesConversion = 180.0 / Math.PI;
            return radians * RadiansToDegreesConversion;
        }
    }
}
