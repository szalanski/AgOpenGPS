namespace AgOpenGPS.Core.Models
{
    public class Background
    {
        public Background(bool isGeoMap)
        {
            IsGeoMap = isGeoMap;
        }

        public bool IsGeoMap { get; }
        public GeoBoundingBox GeoBoundingBox { get; }
    }
}
