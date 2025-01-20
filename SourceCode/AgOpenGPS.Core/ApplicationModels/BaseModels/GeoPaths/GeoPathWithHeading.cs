using System;
using System.Collections.Generic;

namespace AgOpenGPS.Core.ApplicationModels
{
    public class GeoPathWithHeading : GeoPath
    {
        private readonly List<GeoDir> _headings;
        public GeoPathWithHeading()
        {
            _headings = new List<GeoDir>();
        }

        public override GeoCoord this[int i] => throw new NotImplementedException();

        public override void Add(GeoCoord coord)
        {
            throw new InvalidOperationException();
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
