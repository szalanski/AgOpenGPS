using System;

namespace AgOpenGPS.Api.Utilities
{
    /// <summary>
    /// Exponential moving average filter for smoothing noisy sensor data.
    /// Formula: filtered = (previous * weight) + (current * (1 - weight))
    /// </summary>
    public class ExponentialMovingAverageFilter
    {
        private readonly double _weight;
        private double _currentValue;

        /// <summary>
        /// Creates a new exponential moving average filter.
        /// </summary>
        /// <param name="weight">Weight for previous value (0.0 to 1.0). Higher = more smoothing.</param>
        /// <param name="initialValue">Initial filtered value.</param>
        public ExponentialMovingAverageFilter(double weight = 0.75, double initialValue = 0.0)
        {
            if (weight < 0.0 || weight > 1.0)
                throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be between 0.0 and 1.0");

            _weight = weight;
            _currentValue = initialValue;
        }

        /// <summary>
        /// Updates the filter with a new value and returns the filtered result.
        /// </summary>
        public double Update(double newValue)
        {
            _currentValue = (_currentValue * _weight) + (newValue * (1 - _weight));
            return _currentValue;
        }

        /// <summary>
        /// Gets the current filtered value without updating.
        /// </summary>
        public double Current => _currentValue;
    }
}
