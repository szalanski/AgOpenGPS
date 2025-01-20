using System;
using System.Collections.Generic;

namespace AgOpenGPS.Core
{
    public class GeoPolygonWithHeading : GeoPolygon
    {
        private readonly List<GeoDir> _headings;
        public GeoPolygonWithHeading()
        {
            _headings = new List<GeoDir>();
        }

        public override void Add(GeoCoord coord)
        {
            throw new NotImplementedException();
        }

        public void Add(GeoCoord coordDir, GeoDir geoDir)
        {
            _coords.Add(coordDir);
            _headings.Add(geoDir);
        }

        public GeoDir GetHeading(int i)
        {
            return _headings[i];
        }

    }
}
