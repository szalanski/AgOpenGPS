namespace AgOpenGPS.Api.Client.Commands
{
    /// <summary>
    /// Command to start the GPS simulator with initial parameters.
    /// </summary>
    /// <param name="Latitude">Starting latitude in decimal degrees</param>
    /// <param name="Longitude">Starting longitude in decimal degrees</param>
    /// <param name="HeadingDegrees">Initial heading in degrees (0 = North, 90 = East)</param>
    /// <param name="SpeedKmh">Initial speed in kilometers per hour</param>
    public record StartSimulatorCommand(
        double Latitude,
        double Longitude,
        double HeadingDegrees,
        double SpeedKmh) : ICommand;

    /// <summary>
    /// Command to stop the GPS simulator.
    /// </summary>
    public record StopSimulatorCommand() : ICommand;

    /// <summary>
    /// Command to update the simulator's speed.
    /// </summary>
    /// <param name="SpeedKmh">New speed in kilometers per hour</param>
    public record SetSimulatorSpeedCommand(double SpeedKmh) : ICommand;

    /// <summary>
    /// Command to update the simulator's steering angle.
    /// </summary>
    /// <param name="SteerAngle">Steering angle in degrees (positive = right, negative = left)</param>
    public record SetSimulatorSteeringCommand(double SteerAngle) : ICommand;

    /// <summary>
    /// Command to reset the simulator to default state.
    /// </summary>
    public record ResetSimulatorCommand() : ICommand;
}
