using System;
using System.Collections.Generic;

namespace AgOpenGPS
{
    // Stateless curve processing helpers matching CABCurve behavior
    public static class CurveCABTools
    {
        // Perform minimum spacing, interpolation and heading calculation
        public static void Preprocess(ref List<vec3> points, double minSpacing, double interpolationSpacing)
        {
            MakePointMinimumSpacing(ref points, minSpacing);
            InterpolatePoints(ref points, interpolationSpacing);
            CalculateHeadings(ref points);
        }

        // Remove points that are too close to each other
        public static void MakePointMinimumSpacing(ref List<vec3> points, double minSpacing)
        {
            if (points.Count < 2) return;

            List<vec3> spaced = new List<vec3>();
            vec3 last = points[0];
            spaced.Add(last);

            for (int i = 1; i < points.Count; i++)
            {
                double dx = points[i].easting - last.easting;
                double dy = points[i].northing - last.northing;

                if ((dx * dx + dy * dy) >= (minSpacing * minSpacing))
                {
                    spaced.Add(points[i]);
                    last = points[i];
                }
            }

            vec3 final = points[points.Count - 1];
            vec3 compare = spaced[spaced.Count - 1];
            if (glm.DistanceSquared(final, compare) > 0.00001)
            {
                spaced.Add(final);
            }

            points = spaced;
        }

        // Add interpolated points between existing ones
        public static void InterpolatePoints(ref List<vec3> points, double spacingMeters)
        {
            if (points.Count < 2) return;

            List<vec3> result = new List<vec3>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                vec3 a = points[i];
                vec3 b = points[i + 1];
                result.Add(a);

                double dx = b.easting - a.easting;
                double dy = b.northing - a.northing;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                int steps = (int)(distance / spacingMeters);
                for (int j = 1; j < steps; j++)
                {
                    double t = (double)j / steps;
                    double x = a.easting + dx * t;
                    double y = a.northing + dy * t;
                    result.Add(new vec3(x, y, 0));
                }
            }

            result.Add(points[points.Count - 1]);
            points = result;
        }

        // Compute segment-based headings (AgOpenGPS: heading = atan2(dx, dy))
        public static void CalculateHeadings(ref List<vec3> points)
        {
            if (points.Count < 2) return;

            for (int i = 0; i < points.Count - 1; i++)
            {
                double dx = points[i + 1].easting - points[i].easting;
                double dy = points[i + 1].northing - points[i].northing;
                vec3 pt = points[i];
                pt.heading = Math.Atan2(dx, dy); // AgOpenGPS heading: easting over northing
                if (pt.heading < 0) pt.heading += glm.twoPI;
                points[i] = pt;
            }

            vec3 last = points[points.Count - 1];
            last.heading = points[points.Count - 2].heading;
            points[points.Count - 1] = last;
        }

        // Calculate average heading (circular mean)
        public static double ComputeAverageHeading(List<vec3> points)
        {
            if (points.Count == 0) return 0;

            double x = 0;
            double y = 0;

            for (int i = 0; i < points.Count; i++)
            {
                x += Math.Cos(points[i].heading);
                y += Math.Sin(points[i].heading);
            }

            x /= points.Count;
            y /= points.Count;

            double avgHeading = Math.Atan2(y, x);
            if (avgHeading < 0) avgHeading += glm.twoPI;
            return avgHeading;
        }
    }
}
