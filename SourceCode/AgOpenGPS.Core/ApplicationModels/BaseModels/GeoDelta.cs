using System;

namespace AgOpenGPS.Core
{
    public class GeoDelta
    {
        private double _northingDelta;
        private double _eastingDelta;

        public GeoDelta(GeoCoord fromCoord, GeoCoord toCoord)
        {
            _northingDelta = toCoord.Northing - fromCoord.Northing;
            _eastingDelta = toCoord.Easting - fromCoord.Easting;
        }

        public GeoDelta(double northingDelta, double eastingDelta)
        {
            _northingDelta = northingDelta;
            _eastingDelta = eastingDelta;
        }

        public double NorthingDelta => _northingDelta;
        public double EastingDelta => _eastingDelta;

        public double LengthSquared => _northingDelta * _northingDelta + _eastingDelta * _eastingDelta;
        public double Length => Math.Sqrt(LengthSquared);
    }
}
