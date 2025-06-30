using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using AgOpenGPS.Core;
using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Protocols.ISOBUS
{
    // Static helpers to parse GGP and LSG guidance lines
    public static class IsoXmlParserHelpers
    {
        // Parse a GGP node (usually AB or curve lines nested in GPN → LSG → PNT)
        public static void ParseGGPNode(XmlNode node, ApplicationModel appModel, FormGPS mf)
        {
            var gpn = node.SelectSingleNode("GPN");
            var lsg = gpn?.SelectSingleNode("LSG[@A='5']");
            if (gpn == null || lsg == null) return;

            string lineType = gpn.Attributes["C"]?.Value;
            string name = gpn.Attributes["B"]?.Value ?? node.Attributes["B"]?.Value ?? "Unnamed";

            if (lineType == "1" && lsg.ChildNodes.Count >= 2) // AB Line
            {
                ParseABLine(lsg, name, appModel, mf);
            }
            else if (lineType == "3" && lsg.ChildNodes.Count > 2) // Curve
            {
                ParseCurveLine(lsg, name, appModel, mf);
            }
        }

        // Parse a top-level LSG node (v3 style)
        public static void ParseLSGNode(XmlNode lsg, ApplicationModel appModel, FormGPS mf)
        {
            string name = lsg.Attributes["B"]?.Value ?? "Unnamed";
            int pointCount = lsg.SelectNodes("PNT").Count;

            if (pointCount == 2)
            {
                ParseABLine(lsg, name, appModel, mf);
            }
            else if (pointCount > 2)
            {
                ParseCurveLine(lsg, name, appModel, mf);
            }
        }

        private static void ParseABLine(XmlNode lsg, string name, ApplicationModel appModel, FormGPS mf)
        {
            var points = lsg.SelectNodes("PNT");
            if (points.Count < 2) return;

            var ptA = ParseVec2(points[0], appModel);
            var ptB = ParseVec2(points[1], appModel);

            double heading = Math.Atan2(ptB.easting - ptA.easting, ptB.northing - ptA.northing);
            if (heading < 0) heading += glm.twoPI;

            var track = new CTrk
            {
                heading = heading,
                mode = TrackMode.AB,
                ptA = ptA,
                ptB = ptB,
                name = name.Trim()
            };

            mf.trk.gArr.Add(track);
        }

        private static void ParseCurveLine(XmlNode lsg, string name, ApplicationModel appModel, FormGPS mf)
        {
            var points = lsg.SelectNodes("PNT");
            if (points.Count <= 2) return;

            var desList = new List<vec3>();
            foreach (XmlNode pnt in points)
            {
                var geo = ParseGeoCoord(pnt, appModel);
                desList.Add(new vec3(geo));
            }

            mf.curve.MakePointMinimumSpacing(ref desList, 1.6);
            mf.curve.CalculateHeadings(ref desList);

            double x = 0, y = 0;
            foreach (vec3 pt in desList)
            {
                x += Math.Cos(pt.heading);
                y += Math.Sin(pt.heading);
            }
            x /= desList.Count;
            y /= desList.Count;
            double avgHeading = Math.Atan2(y, x);
            if (avgHeading < 0) avgHeading += glm.twoPI;

            mf.curve.AddFirstLastPoints(ref desList);
            mf.curve.CalculateHeadings(ref desList);

            var track = new CTrk
            {
                heading = avgHeading,
                mode = TrackMode.Curve,
                name = string.IsNullOrWhiteSpace(name)
                    ? Math.Round(glm.toDegrees(avgHeading), 1).ToString(CultureInfo.InvariantCulture) + "°" + DateTime.Now.ToString("HH:mm:ss")
                    : name
            };

            foreach (vec3 pt in desList)
            {
                track.curvePts.Add(new vec3(pt));
            }

            mf.trk.gArr.Add(track);
        }

        private static vec2 ParseVec2(XmlNode pnt, ApplicationModel appModel)
        {
            double.TryParse(pnt.Attributes["C"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat);
            double.TryParse(pnt.Attributes["D"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon);
            GeoCoord geo = appModel.LocalPlane.ConvertWgs84ToGeoCoord(new Wgs84(lat, lon));
            return new vec2(geo);
        }

        private static GeoCoord ParseGeoCoord(XmlNode pnt, ApplicationModel appModel)
        {
            double.TryParse(pnt.Attributes["C"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat);
            double.TryParse(pnt.Attributes["D"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon);
            return appModel.LocalPlane.ConvertWgs84ToGeoCoord(new Wgs84(lat, lon));
        }
    }
}
