using AgOpenGPS.Api.Client.Models;

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
    /// Strongly-typed parameters for Start event using value objects.
    /// LocalPlaneOrigin allows client to override coordinate origin (e.g., for field loading scenarios).
    /// </summary>
    public record SimulatorStartData(
        Wgs84Position Position,
        Heading Heading,
        Speed Speed,
        Wgs84Position? LocalPlaneOrigin = null)
    {
        /// <summary>
        /// Gets the effective local plane origin, falling back to Position if not explicitly set.
        /// This allows clients to specify different origins for field loading scenarios
        /// while maintaining backward compatibility (null = use start position).
        /// </summary>
        public Wgs84Position GetEffectiveOrigin() => LocalPlaneOrigin ?? Position;
    };

    /// <summary>
    /// Strongly-typed simulator event containing type and optional typed parameters.
    /// </summary>
    public record SimulatorEvent
    {
        /// <summary>Type of simulator event.</summary>
        public SimulatorEventType Type { get; init; }

        /// <summary>Optional speed value (used by SpeedSet, SpeedSetSmooth events).</summary>
        public Speed? SpeedValue { get; init; }

        /// <summary>Optional speed delta (used by SpeedAdjust event).</summary>
        public double? SpeedDelta { get; init; }

        /// <summary>Optional steering angle (used by SteeringSet event).</summary>
        public SteeringAngle? SteeringValue { get; init; }

        /// <summary>Optional start parameters (used by Start event).</summary>
        public SimulatorStartData? StartData { get; init; }

        // Factory methods for type-safe event creation

        /// <summary>
        /// Create Start event with initial position, heading, and speed.
        /// Optionally specify a different local plane origin (e.g., for field loading scenarios).
        /// </summary>
        /// <param name="position">Simulator start position</param>
        /// <param name="heading">Initial heading</param>
        /// <param name="speed">Initial speed</param>
        /// <param name="localPlaneOrigin">Optional local plane origin (null = use position as origin)</param>
        public static SimulatorEvent Start(Wgs84Position position, Heading heading, Speed speed, Wgs84Position? localPlaneOrigin = null) => new()
        {
            Type = SimulatorEventType.Start,
            StartData = new SimulatorStartData(position, heading, speed, localPlaneOrigin)
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
            SpeedDelta = delta
        };

        /// <summary>Create SpeedSet event (instant speed change).</summary>
        public static SimulatorEvent SpeedSet(Speed speed) => new()
        {
            Type = SimulatorEventType.SpeedSet,
            SpeedValue = speed
        };

        /// <summary>Create SpeedSetSmooth event (gradual speed transition).</summary>
        public static SimulatorEvent SpeedSetSmooth(Speed speed) => new()
        {
            Type = SimulatorEventType.SpeedSetSmooth,
            SpeedValue = speed
        };

        /// <summary>Create SpeedZero event (instant stop).</summary>
        public static SimulatorEvent SpeedZero() => new()
        {
            Type = SimulatorEventType.SpeedZero
        };

        /// <summary>Create SteeringSet event (angle in degrees).</summary>
        public static SimulatorEvent SteeringSet(SteeringAngle angle) => new()
        {
            Type = SimulatorEventType.SteeringSet,
            SteeringValue = angle
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
    /// Provides factory methods for clean, type-safe API with value objects.
    /// </summary>
    /// <example>
    /// // Start simulator with strongly-typed position, heading, and speed
    /// var position = new Wgs84Position(45.0, -93.0);
    /// var heading = new Heading(0.0);
    /// var speed = new Speed(10.0);
    /// await client.SendCommandAsync(UpdateSimulatorCommand.Start(position, heading, speed));
    ///
    /// // Start with explicit local plane origin (for field loading)
    /// var fieldOrigin = new Wgs84Position(45.1, -93.1);
    /// await client.SendCommandAsync(UpdateSimulatorCommand.Start(position, heading, speed, fieldOrigin));
    ///
    /// // Speed up by 1 km/h
    /// await client.SendCommandAsync(UpdateSimulatorCommand.AdjustSpeed(1.0));
    ///
    /// // Set steering with type-safe SteeringAngle
    /// await client.SendCommandAsync(UpdateSimulatorCommand.SetSteering(new SteeringAngle(20.0)));
    ///
    /// // Reverse direction
    /// await client.SendCommandAsync(UpdateSimulatorCommand.ReverseDirection());
    /// </example>
    public record UpdateSimulatorCommand(SimulatorEvent Event) : ICommand
    {
        // Factory methods for clean client API

        /// <summary>
        /// Start simulator with initial position, heading, and speed.
        /// Optionally specify a different local plane origin (e.g., for field loading scenarios).
        /// </summary>
        public static UpdateSimulatorCommand Start(Wgs84Position position, Heading heading, Speed speed, Wgs84Position? localPlaneOrigin = null) =>
            new(SimulatorEvent.Start(position, heading, speed, localPlaneOrigin));

        /// <summary>Stop simulator.</summary>
        public static UpdateSimulatorCommand Stop() =>
            new(SimulatorEvent.Stop());

        /// <summary>Set absolute speed instantly.</summary>
        public static UpdateSimulatorCommand SetSpeed(Speed speed) =>
            new(SimulatorEvent.SpeedSet(speed));

        /// <summary>Set speed with smooth transition.</summary>
        public static UpdateSimulatorCommand SetSpeedSmooth(Speed speed) =>
            new(SimulatorEvent.SpeedSetSmooth(speed));

        /// <summary>Adjust speed by delta value (for repeat button behavior).</summary>
        public static UpdateSimulatorCommand AdjustSpeed(double delta) =>
            new(SimulatorEvent.SpeedAdjust(delta));

        /// <summary>Instant stop (set speed to zero).</summary>
        public static UpdateSimulatorCommand SetSpeedToZero() =>
            new(SimulatorEvent.SpeedZero());

        /// <summary>Set steering angle.</summary>
        public static UpdateSimulatorCommand SetSteering(SteeringAngle angle) =>
            new(SimulatorEvent.SteeringSet(angle));

        /// <summary>Reset steering to center (0 degrees).</summary>
        public static UpdateSimulatorCommand ResetSteering() =>
            new(SimulatorEvent.SteeringReset());

        /// <summary>Reverse direction by 180 degrees.</summary>
        public static UpdateSimulatorCommand ReverseDirection() =>
            new(SimulatorEvent.DirectionReverse());

        /// <summary>Reset position to starting coordinates.</summary>
        public static UpdateSimulatorCommand ResetPosition() =>
            new(SimulatorEvent.PositionReset());

        /// <summary>Full simulator reset.</summary>
        public static UpdateSimulatorCommand Reset() =>
            new(SimulatorEvent.Reset());
    };
}
