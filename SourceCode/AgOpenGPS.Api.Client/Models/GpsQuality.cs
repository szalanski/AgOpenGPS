using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents GPS signal quality metrics.
    /// Groups related quality indicators into a single immutable value object.
    /// </summary>
    public struct GpsQuality
    {
        /// <summary>
        /// Initializes a new instance of the GpsQuality struct.
        /// </summary>
        /// <param name="fixQuality">GPS fix quality indicator</param>
        /// <param name="satellitesTracked">Number of satellites being tracked</param>
        /// <param name="hdop">Horizontal dilution of precision</param>
        /// <param name="age">Age of differential correction in seconds</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public GpsQuality(int fixQuality, int satellitesTracked, double hdop, double age)
        {
            if (satellitesTracked < 0)
                throw new ArgumentOutOfRangeException(nameof(satellitesTracked), "Satellites tracked cannot be negative");

            if (hdop < 0)
                throw new ArgumentOutOfRangeException(nameof(hdop), "HDOP cannot be negative");

            if (age < 0)
                throw new ArgumentOutOfRangeException(nameof(age), "Age cannot be negative");

            FixQuality = fixQuality;
            SatellitesTracked = satellitesTracked;
            Hdop = hdop;
            Age = age;
        }

        /// <summary>
        /// Gets the GPS fix quality indicator.
        /// 0 = No fix
        /// 1 = GPS fix (SPS)
        /// 2 = DGPS fix
        /// 4 = RTK fixed
        /// 5 = RTK float
        /// 6 = Estimated/Dead reckoning
        /// </summary>
        public int FixQuality { get; init; }

        /// <summary>
        /// Gets the number of satellites being tracked.
        /// More satellites generally indicates better position accuracy.
        /// </summary>
        public int SatellitesTracked { get; init; }

        /// <summary>
        /// Gets the Horizontal Dilution of Precision (HDOP).
        /// Lower values indicate better position accuracy.
        /// Typical values: &lt;1 = Ideal, 1-2 = Excellent, 2-5 = Good, 5-10 = Moderate, 10-20 = Fair, &gt;20 = Poor
        /// </summary>
        public double Hdop { get; init; }

        /// <summary>
        /// Gets the age of differential correction in seconds.
        /// Only relevant for DGPS/RTK modes. Lower is better.
        /// </summary>
        public double Age { get; init; }

        /// <summary>
        /// Determines if the GPS has a valid fix.
        /// </summary>
        /// <returns>True if fix quality indicates a valid position</returns>
        public bool HasValidFix()
        {
            return FixQuality > 0;
        }

        /// <summary>
        /// Determines if the GPS fix is RTK (fixed or float).
        /// </summary>
        /// <returns>True if fix is RTK fixed (4) or RTK float (5)</returns>
        public bool IsRtkFix()
        {
            return FixQuality == 4 || FixQuality == 5;
        }

        /// <summary>
        /// Determines if the GPS fix is RTK fixed (highest precision).
        /// </summary>
        /// <returns>True if fix is RTK fixed (4)</returns>
        public bool IsRtkFixed()
        {
            return FixQuality == 4;
        }

        /// <summary>
        /// Returns a string representation of this GPS quality.
        /// </summary>
        public override string ToString()
        {
            string fixType = FixQuality switch
            {
                0 => "No Fix",
                1 => "GPS",
                2 => "DGPS",
                4 => "RTK Fixed",
                5 => "RTK Float",
                6 => "Estimated",
                _ => $"Unknown ({FixQuality})"
            };

            return $"{fixType}, Sats: {SatellitesTracked}, HDOP: {Hdop:F1}, Age: {Age:F1}s";
        }

        /// <summary>
        /// Determines whether two GpsQuality instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is GpsQuality quality &&
                   FixQuality == quality.FixQuality &&
                   SatellitesTracked == quality.SatellitesTracked &&
                   Hdop == quality.Hdop &&
                   Age == quality.Age;
        }

        /// <summary>
        /// Returns a hash code for this GpsQuality.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + FixQuality.GetHashCode();
                hash = hash * 23 + SatellitesTracked.GetHashCode();
                hash = hash * 23 + Hdop.GetHashCode();
                hash = hash * 23 + Age.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Equality operator for GpsQuality.
        /// </summary>
        public static bool operator ==(GpsQuality left, GpsQuality right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for GpsQuality.
        /// </summary>
        public static bool operator !=(GpsQuality left, GpsQuality right)
        {
            return !(left == right);
        }
    }
}
