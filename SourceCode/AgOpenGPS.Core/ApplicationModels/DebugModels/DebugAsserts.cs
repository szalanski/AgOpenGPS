using System.Diagnostics;

namespace AgOpenGPS.Core.ApplicationModels
{
    public class DebugAsserts
    {
        public static void AssertEqual(double a, double b, double epsilon = 0.0001)
        {
            Debug.Assert(b < a + epsilon);
            Debug.Assert(a < b + epsilon);
        }

        public static void AssertEqual(bool a, bool b)
        {
            Debug.Assert(a == b);
        }

        public static void AssertEqual(GeoCoord a, GeoCoord b)
        {
            AssertEqual(a.Northing, b.Northing);
            AssertEqual(a.Easting, b.Easting);
        }

        public static void AssertEqual(GeoDir a, GeoDir b)
        {
            AssertEqual(a.Cos, b.Cos);
            AssertEqual(a.Sin, b.Sin);
        }

        public static void AssertEqual(GeoPath a, GeoPath b)
        {
            DebugAsserts.AssertEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                DebugAsserts.AssertEqual(a[i], b[i]);
            }
        }

        public static void AssertEqual(GeoPolygon a, GeoPolygon b)
        {
            DebugAsserts.AssertEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                DebugAsserts.AssertEqual(a[i], b[i]);
            }
        }
    }
}
