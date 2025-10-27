using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents the complete GNSS (Global Navigation Satellite System) state.
    /// Contains all GPS/GNSS data received from positioning hardware via AgIO.
    /// This includes position, motion, quality, and health metrics.
    /// </summary>
    public class GnssState
    {
        /// <summary>
        /// Gets or sets the WGS84 geographic position (latitude/longitude).
        /// This is the raw position from GPS satellites in the World Geodetic System 1984 coordinate system.
        /// </summary>
        public Wgs84Position WgsPosition { get; set; }

        /// <summary>
        /// Gets or sets the local plane position (easting/northing).
        /// This is the WGS84 position converted to a local flat-earth coordinate system
        /// relative to the field origin, measured in meters.
        /// </summary>
        public LocalPosition LocalPosition { get; set; }

        /// <summary>
        /// Gets or sets the true heading from single antenna GPS (VTG/RMC sentence).
        /// This heading is derived from GPS velocity vector (course over ground).
        /// Only accurate when vehicle is moving.
        /// </summary>
        public Heading HeadingSingle { get; set; }

        /// <summary>
        /// Gets or sets the true heading from dual antenna GPS system.
        /// This heading is measured directly from the baseline between two GPS antennas.
        /// Accurate even when vehicle is stationary.
        /// </summary>
        public Heading HeadingDual { get; set; }

        /// <summary>
        /// Gets or sets the ground speed.
        /// Measured speed over ground from GPS (VTG sentence).
        /// </summary>
        public Speed Speed { get; set; }

        /// <summary>
        /// Gets or sets the altitude above mean sea level (MSL).
        /// Elevation from GPS receiver.
        /// </summary>
        public Altitude Altitude { get; set; }

        /// <summary>
        /// Gets or sets the GPS signal quality metrics.
        /// Includes fix quality, satellites tracked, HDOP, and differential correction age.
        /// </summary>
        public GpsQuality Quality { get; set; }

        /// <summary>
        /// Gets or sets the GPS system health monitoring metrics.
        /// Includes message arrival frequency and watchdog counter.
        /// </summary>
        public GpsHealth Health { get; set; }

        /// <summary>
        /// Initializes a new instance of the GnssState class with default values.
        /// </summary>
        public GnssState()
        {
            // Initialize with safe default values
            WgsPosition = new Wgs84Position(0, 0);
            LocalPosition = new LocalPosition(0, 0);
            HeadingSingle = new Heading(0);
            HeadingDual = new Heading(0);
            Speed = new Speed(0);
            Altitude = new Altitude(0);
            Quality = new GpsQuality(0, 0, 99.9, 0);
            Health = new GpsHealth(0, 0);
        }

        /// <summary>
        /// Returns a string representation of this GNSS state.
        /// </summary>
        public override string ToString()
        {
            return $"GNSS: {WgsPosition}, {Quality.FixQuality}, {Speed.KilometersPerHour:F1} km/h, {Health.GpsHz:F1} Hz";
        }
    }
}
