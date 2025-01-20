namespace AgOpenGPS.Core.ApplicationModels
{
    public class DebugGeoPath : GeoPath
    {
        private readonly GeoPath _goldenPath;
        private DebugGeoPath()
        { }

        public DebugGeoPath(GeoPath goldenPath)
        {
            _goldenPath = goldenPath;
        }

        public override void Add(GeoCoord coord)
        {
            base.Add(coord);
            DebugAsserts.AssertEqual(coord, _goldenPath[Count - 1]);

        }

        public void AssertEqual()
        {
            DebugAsserts.AssertEqual(Count, _goldenPath.Count);
        }
    }
}
