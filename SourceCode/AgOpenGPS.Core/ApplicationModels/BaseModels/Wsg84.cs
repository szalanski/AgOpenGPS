namespace AgOpenGPS.Core
{
    public class Wgs84
    {
        private double _latitude;
        private double _longitude;

        public Wgs84(double latitude, double longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        public double Latitude => _latitude;
        public double Longitude => _longitude;
    }
}
