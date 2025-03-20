using AgOpenGPS.Core.Models;
using Dev4Agriculture.ISO11783.ISOXML.TaskFile;
using Dev4Agriculture.ISO11783.ISOXML;
using System.Collections.Generic;

namespace AgOpenGPS.Protocols.ISOBUS
{
    public class ISO11783_TaskFile_V4
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
            isoxml.VersionMajor = ISO11783TaskDataFileVersionMajor.Version4;
            isoxml.VersionMinor = ISO11783TaskDataFileVersionMinor.Item2;

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
                        PointType = ISOPointType.PartfieldReferencePoint,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });
                }

                polygon.LineString.Add(lineString);

                partfield.PolygonnonTreatmentZoneonly.Add(polygon);
            }

            foreach (CBoundaryList boundarList in bndList)
            {
                if (boundarList.hdLine.Count < 1) continue;

                var polygon = new ISOPolygon
                {
                    PolygonType = ISOPolygonType.Headland
                };

                var lineString = new ISOLineString
                {
                    LineStringType = ISOLineStringType.PolygonExterior
                };

                foreach (vec3 v3 in boundarList.hdLine)
                {
                    Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(v3.ToGeoCoord());
                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = ISOPointType.PartfieldReferencePoint,
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
                    var guidanceGroup = new ISOGuidanceGroup
                    {
                        GuidanceGroupDesignator = track.name
                    };

                    isoxml.IdTable.AddObjectAndAssignIdIfNone(guidanceGroup);

                    var guidancePattern = new ISOGuidancePattern
                    {
                        GuidancePatternId = guidanceGroup.GuidanceGroupId,
                        GuidancePatternDesignator = track.name,
                        GuidancePatternType = ISOGuidancePatternType.AB,
                        GuidancePatternPropagationDirection = ISOGuidancePatternPropagationDirection.Bothdirections,
                        GuidancePatternExtension = ISOGuidancePatternExtension.Frombothfirstandlastpoint,
                        GuidancePatternGNSSMethod = ISOGuidancePatternGNSSMethod.Desktopgenerateddata
                    };

                    var lineString = new ISOLineString
                    {
                        LineStringType = ISOLineStringType.GuidancePattern
                    };

                    GeoCoord pointA = track.ptA.ToGeoCoord();
                    GeoDir heading = new GeoDir(track.heading);
                    Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(pointA - 1000.0 * heading);

                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = ISOPointType.GuidanceReferenceA,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });

                    latLon = localPlane.ConvertGeoCoordToWgs84(pointA + 1000.0 * heading);

                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = ISOPointType.GuidanceReferenceB,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });

                    guidancePattern.LineString.Add(lineString);

                    guidanceGroup.GuidancePattern.Add(guidancePattern);

                    partfield.GuidanceGroup.Add(guidanceGroup);
                }
            }

            if (trk.gArr != null)
            {
                foreach (CTrk track in trk.gArr)
                {
                    var guidanceGroup = new ISOGuidanceGroup
                    {
                        GuidanceGroupDesignator = track.name
                    };

                    isoxml.IdTable.AddObjectAndAssignIdIfNone(guidanceGroup);

                    var guidancePattern = new ISOGuidancePattern
                    {
                        GuidancePatternId = guidanceGroup.GuidanceGroupId,
                        GuidancePatternDesignator = track.name,
                        GuidancePatternType = ISOGuidancePatternType.Curve,
                        GuidancePatternPropagationDirection = ISOGuidancePatternPropagationDirection.Bothdirections,
                        GuidancePatternExtension = ISOGuidancePatternExtension.Frombothfirstandlastpoint,
                        GuidancePatternGNSSMethod = ISOGuidancePatternGNSSMethod.Desktopgenerateddata
                    };

                    var lineString = new ISOLineString
                    {
                        LineStringType = ISOLineStringType.GuidancePattern
                    };

                    for (int j = 0; j < track.curvePts.Count; j++)
                    {
                        Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(track.curvePts[j].ToGeoCoord());

                        var point = new ISOPoint
                        {
                            PointNorth = (decimal)latLon.Latitude,
                            PointEast = (decimal)latLon.Longitude
                        };

                        if (j == 0)
                        {
                            point.PointType = ISOPointType.GuidanceReferenceA;
                        }
                        else if (j == track.curvePts.Count - 1)
                        {
                            point.PointType = ISOPointType.GuidanceReferenceB;
                        }
                        else
                        {
                            point.PointType = ISOPointType.GuidancePoint;
                        }

                        lineString.Point.Add(point);
                    }

                    guidancePattern.LineString.Add(lineString);

                    guidanceGroup.GuidancePattern.Add(guidancePattern);

                    partfield.GuidanceGroup.Add(guidanceGroup);
                }
            }

            isoxml.Data.Partfield.Add(partfield);

            isoxml.Save();
        }
    }
}
