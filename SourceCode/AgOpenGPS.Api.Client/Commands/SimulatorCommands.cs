namespace AgOpenGPS.Api.Client.Commands
{
    /// <summary>
    /// Event types for simulator control.
    /// </summary>
    public enum SimulatorEventType
    {
        /// <summary>Start simulator with initial parameters.</summary>
        Start,
        /// <summary>Stop simulator.</summary>
        Stop,
        /// <summary>Adjust speed by delta value (for repeat button behavior).</summary>
        SpeedAdjust,
        /// <summary>Set absolute speed instantly.</summary>
        SpeedSet,
        /// <summary>Set speed with smooth transition.</summary>
        SpeedSetSmooth,
        /// <summary>Instant stop (set speed to zero).</summary>
        SpeedZero,
        /// <summary>Set steering angle.</summary>
        SteeringSet,
        /// <summary>Reset steering to center (0 degrees).</summary>
        SteeringReset,
        /// <summary>Reverse direction by 180 degrees.</summary>
        DirectionReverse,
        /// <summary>Reset position to starting coordinates.</summary>
        PositionReset,
        /// <summary>Full simulator reset.</summary>
        Reset
    }

    /// <summary>
    /// Strongly-typed parameters for Start event.
    /// </summary>
    public record SimulatorStartData(
        double Latitude,
        double Longitude,
        double Heading,
        double Speed);

    /// <summary>
    /// Strongly-typed simulator event containing type, optional value, and optional start data.
    /// </summary>
    public record SimulatorEvent
    {
        /// <summary>Type of simulator event.</summary>
        public SimulatorEventType Type { get; init; }

        /// <summary>Optional numeric value (used by speed/steering events).</summary>
        public double? Value { get; init; }

        /// <summary>Optional start parameters (used by Start event).</summary>
        public SimulatorStartData? StartData { get; init; }

        // Factory methods for type-safe event creation

        /// <summary>Create Start event with initial position, heading, and speed.</summary>
        public static SimulatorEvent Start(double lat, double lon, double heading, double speed) => new()
        {
            Type = SimulatorEventType.Start,
            StartData = new SimulatorStartData(lat, lon, heading, speed)
        };

        /// <summary>Create Stop event.</summary>
        public static SimulatorEvent Stop() => new()
        {
            Type = SimulatorEventType.Stop
        };

        /// <summary>Create SpeedAdjust event (delta in km/h).</summary>
        public static SimulatorEvent SpeedAdjust(double delta) => new()
        {
            Type = SimulatorEventType.SpeedAdjust,
            Value = delta
        };

        /// <summary>Create SpeedSet event (instant speed change).</summary>
        public static SimulatorEvent SpeedSet(double speed) => new()
        {
            Type = SimulatorEventType.SpeedSet,
            Value = speed
        };

        /// <summary>Create SpeedSetSmooth event (gradual speed transition).</summary>
        public static SimulatorEvent SpeedSetSmooth(double speed) => new()
        {
            Type = SimulatorEventType.SpeedSetSmooth,
            Value = speed
        };

        /// <summary>Create SpeedZero event (instant stop).</summary>
        public static SimulatorEvent SpeedZero() => new()
        {
            Type = SimulatorEventType.SpeedZero
        };

        /// <summary>Create SteeringSet event (angle in degrees).</summary>
        public static SimulatorEvent SteeringSet(double angle) => new()
        {
            Type = SimulatorEventType.SteeringSet,
            Value = angle
        };

        /// <summary>Create SteeringReset event (center steering).</summary>
        public static SimulatorEvent SteeringReset() => new()
        {
            Type = SimulatorEventType.SteeringReset
        };

        /// <summary>Create DirectionReverse event (flip heading 180 degrees).</summary>
        public static SimulatorEvent DirectionReverse() => new()
        {
            Type = SimulatorEventType.DirectionReverse
        };

        /// <summary>Create PositionReset event (return to start coordinates).</summary>
        public static SimulatorEvent PositionReset() => new()
        {
            Type = SimulatorEventType.PositionReset
        };

        /// <summary>Create Reset event (full simulator reset).</summary>
        public static SimulatorEvent Reset() => new()
        {
            Type = SimulatorEventType.Reset
        };
    }

    /// <summary>
    /// Unified simulator command - single command for all simulator interactions.
    /// Uses event object pattern with factory methods for clean, type-safe API.
    /// </summary>
    /// <example>
    /// // Start simulator
    /// await client.SendCommandAsync(new UpdateSimulatorCommand(
    ///     SimulatorEvent.Start(45.0, -93.0, 0.0, 10.0)));
    ///
    /// // Speed up by 1 km/h
    /// await client.SendCommandAsync(new UpdateSimulatorCommand(
    ///     SimulatorEvent.SpeedAdjust(1.0)));
    ///
    /// // Reverse direction
    /// await client.SendCommandAsync(new UpdateSimulatorCommand(
    ///     SimulatorEvent.DirectionReverse()));
    /// </example>
    public record UpdateSimulatorCommand(SimulatorEvent Event) : ICommand;
}
