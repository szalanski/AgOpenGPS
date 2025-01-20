namespace AgOpenGPS.Core
{
    public struct GeoCoordDir
    {
        private GeoCoord _coord;
        private GeoDir _direction;

        public GeoCoordDir(GeoCoord coord, GeoDir direction)
        {
            _direction = direction;
            _coord = coord;
        }

        public GeoCoord Coord => _coord;
        public GeoDir Direction => _direction;
    }
}
