namespace AgOpenGPS.Api.Client.Models
{
    /// <summary>
    /// Vehicle control feedback state.
    /// Contains real-time operational feedback from vehicle actuators and control systems.
    /// Grouped by domain (like GnssState groups GPS data).
    /// </summary>
    public class ControlState
    {
        /// <summary>
        /// Actual steering angle from hardware or simulator (smoothed over time).
        /// Positive = right turn, Negative = left turn, Zero = straight ahead.
        /// Used for wheel rendering and steering visualization.
        /// </summary>
        public SteeringAngle ActualSteeringAngle { get; set; } = SteeringAngle.Zero;

        // Future extensions:
        // public SectionControlState? Sections { get; set; }      // Per-section on/off states, timers
        // public bool IsAutoSteerEngaged { get; set; }            // Auto-steer engagement status
        // public ImplementState? Implement { get; set; }          // Raise/lower position, pressure
        // public SpeedControlState? Speed { get; set; }           // Actual vs target speed
        // public ImuState? Imu { get; set; }                      // Roll, pitch, yaw rate from AHRS
    }
}
