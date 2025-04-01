using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Core.ViewModels
{

    public class LongDistanceViewModel : DayNightAndUnitsViewModel
    {
        private readonly Distance _distance;
        public LongDistanceViewModel(Distance distance)
        {
            _distance = distance;
        }

        public LongDistanceViewModel(double distance) : this (new Distance(distance))
        {
        }

        public double DistanceInMeters => _distance.InMeters;

        public double DistanceInKm => _distance.InKilometers;
        public double DistanceInMiles => _distance.InMiles;

        public double DisplayedDistance => IsMetric ? DistanceInKm : DistanceInMiles;

    }
}
