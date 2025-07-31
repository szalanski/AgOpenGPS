using AgOpenGPS.Core;
using AgOpenGPS.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace AgOpenGPS.Protocols.ISOBUS
{
    // Handles all logic for building a field from ISOXML data
    public class IsoXmlFieldImporter
    {
        private readonly XmlNodeList _fieldParts;
        private readonly string _fieldName;
        private readonly FormGPS _mf;
        private readonly ApplicationModel _appModel;
        private double _latSum = 0, _lonSum = 0;
        private int _coordCount = 0;

        public IsoXmlFieldImporter(XmlNodeList fieldParts, string fieldName, ApplicationModel appModel, FormGPS mf)
        {
            _fieldParts = fieldParts;
            _fieldName = fieldName;
            _appModel = appModel;
            _mf = mf;
        }

        // Try to extract a center WGS84 point from PLN or GGP
        public bool TryExtractOrigin(out Wgs84 origin)
        {
            origin = new Wgs84();

            foreach (XmlNode node in _fieldParts)
            {
                if (node.Name == "PLN" && node.Attributes["A"]?.Value == "1")
                {
                    AccumulatePntCoordinates(node);
                }
            }

            if (_coordCount == 0)
            {
                foreach (XmlNode node in _fieldParts)
                {
                    if (node.Name == "GGP")
                    {
                        var lsg = node.SelectSingleNode("GPN/LSG");
                        if (lsg != null) AccumulatePntCoordinates(lsg.ParentNode);
                    }
                }
            }

            if (_coordCount == 0) return false;

            double avgLat = _latSum / _coordCount;
            double avgLon = _lonSum / _coordCount;
            origin = new Wgs84(avgLat, avgLon);
            return true;
        }

        // Build the outer boundary and inner boundaries
        public void BuildBoundaries()
        {
            bool outerBuilt = false;

            foreach (XmlNode node in _fieldParts)
            {
                if (node.Name != "PLN") continue;

                string type = node.Attributes["A"]?.Value;

                if ((type == "1" || type == "9") && !outerBuilt)
                {
                    if (node.SelectSingleNode("LSG[@A='1']") is XmlNode lsg)
                    {
                        var boundary = ParseBoundaryFromLSG(lsg);
                        _mf.bnd.bndList.Add(boundary);

                        int idx = _mf.bnd.bndList.Count - 1;
                        boundary.CalculateFenceArea(idx);
                        boundary.FixFenceLine(idx);

                        outerBuilt = true;
                    }
                }
                else if (type == "3" || type == "4" || type == "6")
                {
                    if (node.SelectSingleNode("LSG[@A='1']") is XmlNode lsg)
                    {
                        var boundary = ParseBoundaryFromLSG(lsg);
                        _mf.bnd.bndList.Add(boundary);

                        int idx = _mf.bnd.bndList.Count - 1;
                        boundary.CalculateFenceArea(idx);
                        boundary.FixFenceLine(idx);
                    }
                }
            }
        }


        // Build headland boundary if present
        public void BuildHeadland()
        {
            if (_mf.bnd.bndList.Count == 0 || _mf.bnd.bndList[0].hdLine.Count > 0) return;

            foreach (XmlNode node in _fieldParts)
            {
                if (node.Name == "PLN" && node.Attributes["A"]?.Value == "10")
                {
                    if (node.SelectSingleNode("LSG[@A='1']") is XmlNode lsg)
                    {
                        var headland = ParseVec3ListFromLSG(lsg);
                        _mf.curve.CalculateHeadings(ref headland);
                        _mf.bnd.bndList[0].hdLine.AddRange(headland);
                        break;
                    }
                }
            }
        }

        // Parse guidance lines (AB and Curves) from ISOXML
        public void BuildGuidanceLines()
        {
            foreach (XmlNode node in _fieldParts)
            {
                if (node.Name == "GGP")
                {
                    IsoXmlParserHelpers.ParseGGPNode(node, _appModel, _mf);
                }
                else if (node.Name == "LSG" && node.Attributes["A"]?.Value == "5")
                {
                    IsoXmlParserHelpers.ParseLSGNode(node, _appModel, _mf);
                }
            }
        }
        private void AccumulatePntCoordinates(XmlNode parent)
        {
            foreach (XmlNode pnt in parent.SelectNodes(".//PNT"))
            {
                if (double.TryParse(pnt.Attributes["C"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(pnt.Attributes["D"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                {
                    _latSum += lat;
                    _lonSum += lon;
                    _coordCount++;
                }
            }
        }

        // Parse LSG node into a CBoundaryList with vec3 points
        private CBoundaryList ParseBoundaryFromLSG(XmlNode lsg)
        {
            var list = new CBoundaryList();

            foreach (XmlNode pnt in lsg.SelectNodes("PNT"))
            {
                if (double.TryParse(pnt.Attributes["C"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(pnt.Attributes["D"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                {
                    GeoCoord geo = _appModel.LocalPlane.ConvertWgs84ToGeoCoord(new Wgs84(lat, lon));
                    list.fenceLine.Add(new vec3(geo));
                }
            }

            return list;
        }


        private List<vec3> ParseVec3ListFromLSG(XmlNode lsg)
        {
            var list = new List<vec3>();
            foreach (XmlNode pnt in lsg.SelectNodes("PNT"))
            {
                if (double.TryParse(pnt.Attributes["C"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(pnt.Attributes["D"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                {
                    GeoCoord geo = _appModel.LocalPlane.ConvertWgs84ToGeoCoord(new Wgs84(lat, lon));
                    list.Add(new vec3(geo));
                }
            }
            return list;
        }
    }
}
