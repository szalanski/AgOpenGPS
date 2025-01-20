namespace AgOpenGPS.Core
{
    public  class DebugGeoPolygon : GeoPolygon
    {
        private readonly GeoPolygon _goldenPolygon;
        private DebugGeoPolygon()
        { }

        public DebugGeoPolygon(GeoPolygon goldenPolygon)
        {
            _goldenPolygon = goldenPolygon;
        }

        public override void Add(GeoCoord coord)
        {
            base.Add(coord);
            DebugAsserts.AssertEqual(coord, _goldenPolygon[Count - 1]);

        }

        public void AssertEqual()
        {
            DebugAsserts.AssertEqual(Count, _goldenPolygon.Count);
        }
    }
}
