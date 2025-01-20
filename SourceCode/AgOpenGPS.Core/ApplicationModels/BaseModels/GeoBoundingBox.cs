using System;
using System.Diagnostics;

namespace AgOpenGPS.Core.ApplicationModels
{
    public struct GeoBoundingBox
    {
        private GeoCoord _minCoord;
        private GeoCoord _maxCoord;
        private bool _isEmpty;

        static public GeoBoundingBox CreateBoundingBox()
        {
            GeoCoord minCoord = new GeoCoord(double.MaxValue, double.MaxValue);
            GeoCoord maxCoord = new GeoCoord(double.MinValue, double.MinValue);
            return new GeoBoundingBox(minCoord, maxCoord);
        }

        public GeoBoundingBox(GeoCoord minCoord, GeoCoord maxCoord)
        {
            _minCoord = minCoord;
            _maxCoord = maxCoord;
            _isEmpty = true;
        }

        public bool IsEmpty => _isEmpty;
        public double MinNorthing => _minCoord.Northing;
        public double MaxNorthing => _maxCoord.Northing;
        public double MinEasting => _minCoord.Easting;
        public double MaxEasting => _maxCoord.Easting;

        public void Include(GeoCoord geoCoord)
        {
            _minCoord = _minCoord.Min(geoCoord);
            _maxCoord = _maxCoord.Max(geoCoord);
            _isEmpty = false;
        }

        public bool IsInside(GeoCoord testCoord)
        {
            return
                _minCoord.Northing <= testCoord.Northing && testCoord.Northing <= _maxCoord.Northing &&
                _minCoord.Easting <= testCoord.Easting && testCoord.Easting <= _maxCoord.Easting;
        }
    }

}
