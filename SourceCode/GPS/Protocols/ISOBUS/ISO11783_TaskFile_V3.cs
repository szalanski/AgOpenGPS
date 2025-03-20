using AgOpenGPS.Core.Models;
using System.Collections.Generic;
using Dev4Agriculture.ISO11783.ISOXML;
using Dev4Agriculture.ISO11783.ISOXML.TaskFile;

namespace AgOpenGPS.Protocols.ISOBUS
{
    public class ISO11783_TaskFile_V3
    {
        public static void Export(
            string directoryName,
            string designator,
            int area,
            List<CBoundaryList> bndList,
            LocalPlane localPlane,
            CTrack trk)
        {
            var isoxml = ISOXML.Create(directoryName);

            isoxml.DataTransferOrigin = ISO11783TaskDataFileDataTransferOrigin.FMIS;
            isoxml.ManagementSoftwareManufacturer = "AgOpenGPS";
            isoxml.ManagementSoftwareVersion = "1.4.0";
            isoxml.VersionMajor = ISO11783TaskDataFileVersionMajor.Version3;
            isoxml.VersionMinor = ISO11783TaskDataFileVersionMinor.Item3;

            var partfield = new ISOPartfield();
            isoxml.IdTable.AddObjectAndAssignIdIfNone(partfield);
            partfield.PartfieldDesignator = designator;
            partfield.PartfieldArea = (ulong)area;

            for (int i = 0; i < bndList.Count; i++)
            {
                var polygon = new ISOPolygon
                {
                    PolygonType = i == 0 ? ISOPolygonType.PartfieldBoundary : ISOPolygonType.Obstacle
                };

                var lineString = new ISOLineString
                {
                    LineStringType = ISOLineStringType.PolygonExterior
                };

                foreach (vec2 v2 in bndList[i].fenceLineEar)
                {
                    Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(v2.ToGeoCoord());
                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = ISOPointType.other,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });
                }

                polygon.LineString.Add(lineString);

                partfield.PolygonnonTreatmentZoneonly.Add(polygon);
            }

            foreach (CBoundaryList boudaryList in bndList)
            {
                if (boudaryList.hdLine.Count < 1) continue;

                var polygon = new ISOPolygon
                {
                    PolygonType = ISOPolygonType.Headland
                };

                var lineString = new ISOLineString
                {
                    LineStringType = ISOLineStringType.PolygonExterior
                };

                foreach (vec3 v3 in boudaryList.hdLine)
                {
                    Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(v3.ToGeoCoord());
                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = ISOPointType.other,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });
                }

                polygon.LineString.Add(lineString);

                partfield.PolygonnonTreatmentZoneonly.Add(polygon);
            }

            if (trk.gArr != null)
            {
                foreach (CTrk track in trk.gArr)
                {
                    var lineString = new ISOLineString
                    {
                        LineStringType = ISOLineStringType.GuidancePattern,
                        LineStringDesignator = track.name
                    };

                    GeoCoord pointA = track.ptA.ToGeoCoord();
                    GeoDir heading = new GeoDir(track.heading);
                    Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(pointA - 1000.0 * heading);

                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = ISOPointType.other,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });

                    latLon = localPlane.ConvertGeoCoordToWgs84(pointA + 1000.0 * heading);

                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = ISOPointType.other,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });

                    partfield.LineString.Add(lineString);
                }
            }

            if (trk.gArr != null)
            {
                foreach (CTrk track in trk.gArr)
                {
                    var lineString = new ISOLineString
                    {
                        LineStringType = ISOLineStringType.GuidancePattern,
                        LineStringDesignator = track.name
                    };

                    foreach (vec3 v3 in track.curvePts)
                    {
                        Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(v3.ToGeoCoord());

                        lineString.Point.Add(new ISOPoint
                        {
                            PointType = ISOPointType.other,
                            PointNorth = (decimal)latLon.Latitude,
                            PointEast = (decimal)latLon.Longitude
                        });
                    }

                    partfield.LineString.Add(lineString);
                }
            }

            isoxml.Data.Partfield.Add(partfield);

            isoxml.Save();
        }
    }
}
