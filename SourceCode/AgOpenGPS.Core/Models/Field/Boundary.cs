using AgOpenGPS.Core.Models;
using System.Collections.Generic;

namespace AgOpenGPS.Core.Models
{
    public class Boundary
    {
        private readonly List<BoundaryPolygon> _innerBoundaries;

        public Boundary()
        {
            _innerBoundaries = new List<BoundaryPolygon>();
        }

        public BoundaryPolygon OuterBoundary { get; set; }
        public List<BoundaryPolygon> InnerBoundaries => _innerBoundaries;

        public double Area => OuterBoundary?.Area ?? 0.0;

    }
}
