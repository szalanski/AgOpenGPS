using AgOpenGPS.Core.Models;
using AgOpenGPS.Core;
using AgOpenGPS.Protocols.ISOBUS;
using AgOpenGPS;
using System.Collections.Generic;
using System.Xml;

public class IsoXmlFieldImporter
{
    private readonly XmlNodeList _fieldParts;
    private readonly ApplicationModel _appModel;

    public IsoXmlFieldImporter(XmlNodeList fieldParts, ApplicationModel appModel)
    {
        _fieldParts = fieldParts;
        _appModel = appModel;
    }

    public bool TryGetOrigin(out Wgs84 origin) =>
        IsoXmlParserHelpers.TryExtractOrigin(_fieldParts, out origin);

    public List<CBoundaryList> GetBoundaries()
    {
        var result = new List<CBoundaryList>();
        foreach (XmlNode node in _fieldParts)
        {
            if (node.Name == "PLN" && (node.Attributes["A"]?.Value == "1" || node.Attributes["A"]?.Value == "9"))
            {
                var lsg = node.SelectSingleNode("LSG[@A='1']");
                if (lsg != null)
                    result.Add(IsoXmlParserHelpers.ParseBoundaryFromLSG(lsg, _appModel));
            }
            else if (node.Name == "PLN" && (node.Attributes["A"]?.Value == "3" || node.Attributes["A"]?.Value == "4" || node.Attributes["A"]?.Value == "6"))
            {
                var lsg = node.SelectSingleNode("LSG[@A='1']");
                if (lsg != null)
                    result.Add(IsoXmlParserHelpers.ParseBoundaryFromLSG(lsg, _appModel));
            }
        }

        return result;
    }

    public List<vec3> GetHeadland() =>
        IsoXmlParserHelpers.ParseHeadland(_fieldParts, _appModel);

    public List<CTrk> GetGuidanceLines() =>
        IsoXmlParserHelpers.ParseAllGuidanceLines(_fieldParts, _appModel);
}
