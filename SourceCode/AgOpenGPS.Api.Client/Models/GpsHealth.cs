using System;

namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Represents GPS system health monitoring metrics.
    /// Tracks message arrival frequency and packet counter for GPS loss detection.
    /// </summary>
    public struct GpsHealth
    {
        /// <summary>
        /// Initializes a new instance of the GpsHealth struct.
        /// </summary>
        /// <param name="gpsHz">GPS message arrival frequency in Hertz</param>
        /// <param name="sentenceCounter">Sentence counter for GPS watchdog (increments when no GPS received)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are out of valid range</exception>
        public GpsHealth(double gpsHz, uint sentenceCounter)
        {
            if (gpsHz < 0)
                throw new ArgumentOutOfRangeException(nameof(gpsHz), "GPS frequency cannot be negative");

            GpsHz = gpsHz;
            SentenceCounter = sentenceCounter;
        }

        /// <summary>
        /// Gets the GPS message arrival frequency in Hertz.
        /// Typical values: 1 Hz (basic GPS), 5 Hz, 10 Hz (precision agriculture), 20 Hz (high-rate)
        /// Lower frequency may indicate GPS issues or communication problems.
        /// </summary>
        public double GpsHz { get; init; }

        /// <summary>
        /// Gets the sentence counter used for GPS loss detection.
        /// This counter increments when no GPS data is received.
        /// Value is reset to 0 when valid GPS data arrives.
        /// High values indicate GPS signal loss or communication failure.
        /// </summary>
        public uint SentenceCounter { get; init; }

        /// <summary>
        /// Determines if GPS data is being received at an acceptable rate.
        /// </summary>
        /// <param name="minimumHz">Minimum acceptable frequency (default 1 Hz)</param>
        /// <returns>True if GPS frequency is above minimum threshold</returns>
        public bool IsReceivingData(double minimumHz = 1.0)
        {
            return GpsHz >= minimumHz;
        }

        /// <summary>
        /// Determines if GPS signal is lost based on sentence counter.
        /// </summary>
        /// <param name="threshold">Sentence counter threshold for signal loss (default 20)</param>
        /// <returns>True if sentence counter exceeds threshold</returns>
        public bool IsSignalLost(uint threshold = 20)
        {
            return SentenceCounter > threshold;
        }

        /// <summary>
        /// Returns a string representation of this GPS health status.
        /// </summary>
        public override string ToString()
        {
            return $"GPS: {GpsHz:F1} Hz, Counter: {SentenceCounter}";
        }

        /// <summary>
        /// Determines whether two GpsHealth instances are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is GpsHealth health &&
                   GpsHz == health.GpsHz &&
                   SentenceCounter == health.SentenceCounter;
        }

        /// <summary>
        /// Returns a hash code for this GpsHealth.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + GpsHz.GetHashCode();
                hash = hash * 23 + SentenceCounter.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Equality operator for GpsHealth.
        /// </summary>
        public static bool operator ==(GpsHealth left, GpsHealth right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for GpsHealth.
        /// </summary>
        public static bool operator !=(GpsHealth left, GpsHealth right)
        {
            return !(left == right);
        }
    }
}
