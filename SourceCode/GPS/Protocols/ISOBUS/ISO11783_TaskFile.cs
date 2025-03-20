using System.Collections.Generic;
using AgOpenGPS.Core.Models;
using Dev4Agriculture.ISO11783.ISOXML.TaskFile;
using Dev4Agriculture.ISO11783.ISOXML;
using System;

namespace AgOpenGPS.Protocols.ISOBUS
{
    public class ISO11783_TaskFile
    {
        public enum Version { V3, V4 }

        public static void Export(
            string directoryName,
            string designator,
            int area,
            List<CBoundaryList> bndList,
            LocalPlane localPlane,
            CTrack trk,
            Version version)
        {
            if (!Enum.IsDefined(typeof(Version), version))
                throw new ArgumentOutOfRangeException(nameof(version), version, "Invalid version");

            var isoxml = ISOXML.Create(directoryName);

            SetFileInformation(isoxml, version);
            AddPartfield(isoxml, designator, area, bndList, localPlane, trk, version);

            isoxml.Save();
        }

        private static void SetFileInformation(ISOXML isoxml, Version version)
        {
            isoxml.DataTransferOrigin = ISO11783TaskDataFileDataTransferOrigin.FMIS;
            isoxml.ManagementSoftwareManufacturer = "AgOpenGPS";
            isoxml.ManagementSoftwareVersion = Program.Version;

            switch (version)
            {
                case Version.V3:
                    isoxml.VersionMajor = ISO11783TaskDataFileVersionMajor.Version3;
                    isoxml.VersionMinor = ISO11783TaskDataFileVersionMinor.Item3;
                    break;

                case Version.V4:
                    isoxml.VersionMajor = ISO11783TaskDataFileVersionMajor.Version4;
                    isoxml.VersionMinor = ISO11783TaskDataFileVersionMinor.Item2;
                    break;
            }
        }

        private static void AddPartfield(
            ISOXML isoxml,
            string designator,
            int area,
            List<CBoundaryList> bndList,
            LocalPlane localPlane,
            CTrack trk,
            Version version)
        {
            var partfield = new ISOPartfield();
            isoxml.IdTable.AddObjectAndAssignIdIfNone(partfield);
            partfield.PartfieldDesignator = designator;
            partfield.PartfieldArea = (ulong)area;

            AddBoundary(partfield, bndList, localPlane, version);
            AddHeadland(partfield, bndList, localPlane, version);
            AddABLines(isoxml, partfield, trk, localPlane, version);
            AddCurves(isoxml, partfield, trk, localPlane, version);

            isoxml.Data.Partfield.Add(partfield);
        }

        private static void AddBoundary(ISOPartfield partfield, List<CBoundaryList> bndList, LocalPlane localPlane, Version version)
        {
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
                        PointType = version == Version.V4 ? ISOPointType.PartfieldReferencePoint : ISOPointType.other,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });
                }

                polygon.LineString.Add(lineString);

                partfield.PolygonnonTreatmentZoneonly.Add(polygon);
            }
        }

        private static void AddHeadland(ISOPartfield partfield, List<CBoundaryList> bndList, LocalPlane localPlane, Version version)
        {
            foreach (CBoundaryList boundaryList in bndList)
            {
                if (boundaryList.hdLine.Count < 1) continue;

                var polygon = new ISOPolygon
                {
                    PolygonType = ISOPolygonType.Headland
                };

                var lineString = new ISOLineString
                {
                    LineStringType = ISOLineStringType.PolygonExterior
                };

                foreach (vec3 v3 in boundaryList.hdLine)
                {
                    Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(v3.ToGeoCoord());
                    lineString.Point.Add(new ISOPoint
                    {
                        PointType = version == Version.V4 ? ISOPointType.PartfieldReferencePoint : ISOPointType.other,
                        PointNorth = (decimal)latLon.Latitude,
                        PointEast = (decimal)latLon.Longitude
                    });
                }

                polygon.LineString.Add(lineString);

                partfield.PolygonnonTreatmentZoneonly.Add(polygon);
            }
        }

        private static void AddABLines(ISOXML isoxml, ISOPartfield partfield, CTrack trk, LocalPlane localPlane, Version version)
        {
            if (trk.gArr == null) return;

            foreach (CTrk track in trk.gArr)
            {
                if (track.mode != TrackMode.AB) continue;

                switch (version)
                {
                    case Version.V3:
                        {
                            var lineString = CreateABLineString(track, localPlane, version);
                            lineString.LineStringDesignator = track.name;
                            partfield.LineString.Add(lineString);
                        }
                        break;

                    case Version.V4:
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

                            var lineString = CreateABLineString(track, localPlane, version);
                            guidancePattern.LineString.Add(lineString);

                            guidanceGroup.GuidancePattern.Add(guidancePattern);

                            partfield.GuidanceGroup.Add(guidanceGroup);
                        }
                        break;
                }
            }
        }

        private static ISOLineString CreateABLineString(CTrk track, LocalPlane localPlane, Version version)
        {
            var lineString = new ISOLineString
            {
                LineStringType = ISOLineStringType.GuidancePattern
            };

            GeoCoord pointA = track.ptA.ToGeoCoord();
            GeoDir heading = new GeoDir(track.heading);
            Wgs84 latLon = localPlane.ConvertGeoCoordToWgs84(pointA - 1000.0 * heading);

            lineString.Point.Add(new ISOPoint
            {
                PointType = version == Version.V4 ? ISOPointType.GuidanceReferenceA : ISOPointType.other,
                PointNorth = (decimal)latLon.Latitude,
                PointEast = (decimal)latLon.Longitude
            });

            latLon = localPlane.ConvertGeoCoordToWgs84(pointA + 1000.0 * heading);

            lineString.Point.Add(new ISOPoint
            {
                PointType = version == Version.V4 ? ISOPointType.GuidanceReferenceB : ISOPointType.other,
                PointNorth = (decimal)latLon.Latitude,
                PointEast = (decimal)latLon.Longitude
            });

            return lineString;
        }

        private static void AddCurves(ISOXML isoxml, ISOPartfield partfield, CTrack trk, LocalPlane localPlane, Version version)
        {
            if (trk.gArr == null) return;

            foreach (CTrk track in trk.gArr)
            {
                if (track.mode != TrackMode.Curve) continue;

                switch (version)
                {
                    case Version.V3:
                        {
                            var lineString = CreateCurveLineString(track, localPlane, version);
                            lineString.LineStringDesignator = track.name;
                            partfield.LineString.Add(lineString);
                        }
                        break;

                    case Version.V4:
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

                            var lineString = CreateCurveLineString(track, localPlane, version);
                            guidancePattern.LineString.Add(lineString);

                            guidanceGroup.GuidancePattern.Add(guidancePattern);

                            partfield.GuidanceGroup.Add(guidanceGroup);
                        }
                        break;
                }
            }
        }

        private static ISOLineString CreateCurveLineString(CTrk track, LocalPlane localPlane, Version version)
        {
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

                if (version == Version.V4)
                {
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
                }
                else
                {
                    point.PointType = ISOPointType.other;
                }

                lineString.Point.Add(point);
            }

            return lineString;
        }
    }
}
