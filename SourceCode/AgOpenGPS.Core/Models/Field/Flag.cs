namespace AgOpenGPS.Core.Models
{
    public enum FlagColor { Red = 0, Green = 1, Yellow = 2}

    public class Flag
    {
        public Flag(Wgs84 wgs84, GeoCoord geoCoord, GeoDir heading, FlagColor flagColor, int uniqueNumber, string notes)
        {
            Wgs84 = new Wgs84(wgs84.Latitude, wgs84.Longitude);
            GeoCoordDir = new GeoCoordDir(geoCoord, heading);
            FlagColor = flagColor;
            UniqueNumber = uniqueNumber;
            Notes = notes;
        }

        public  Wgs84 Wgs84 { get; }
        public GeoCoordDir GeoCoordDir { get; }

        public double Latitude => Wgs84.Latitude;
        public double Longitude => Wgs84.Longitude;
        public GeoCoord GeoCoord => GeoCoordDir.Coord;
        public GeoDir Heading => GeoCoordDir.Direction;
        public double Northing => GeoCoord.Northing;
        public double Easting => GeoCoord.Easting;

        public FlagColor FlagColor { get; }
        public int UniqueNumber { get; set; }

        public string Notes { get; set;}

        public string ColorAsXbgrHexString()
        {
            if (FlagColor.Green == FlagColor)
                return "ff44ff00";
            if (FlagColor.Yellow == FlagColor)
                return "ff44ffff";
            // Assume Red
            return "ff4400ff";
        }
    }
}