using System.Collections.Generic;

namespace AgOpenGPS.Core.Models
{
    public class TramLines
    {
        private GeoPolygon _outerTrack;
        private GeoPolygon _innerTrack;
        private List<GeoPath> _tramList = new List<GeoPath>();

        public TramLines()
        {
        }

        public GeoPolygon OuterTrack
        {
            get { return _outerTrack; }
            set { _outerTrack = value; }
        }

        public GeoPolygon InnerTrack
        {
            get { return _innerTrack; }
            set { _innerTrack = value; }
        }

        public List<GeoPath> TramList
        {
            get { return _tramList; }
            set { _tramList = value; }
        }

        public void Clear()
        {
            _outerTrack = null;
            _innerTrack = null;
            _tramList.Clear();
        }

        public bool IsEmpty => 0 == TramList.Count && null == _outerTrack;

    }

}
