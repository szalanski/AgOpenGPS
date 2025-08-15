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

    public List<CBoundaryList> GetBoundaries() =>
        IsoXmlParserHelpers.ParseBoundaries(_fieldParts, _appModel);

    public List<vec3> GetHeadland() =>
        IsoXmlParserHelpers.ParseHeadland(_fieldParts, _appModel);

    public List<CTrk> GetGuidanceLines() =>
        IsoXmlParserHelpers.ParseAllGuidanceLines(_fieldParts, _appModel);
}
