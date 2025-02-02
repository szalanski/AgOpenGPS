namespace AgOpenGPS.Core.Models
{
    public enum VehicleType
    {
        Tractor = 0,
        Harvester = 1,
        Articulated = 2
    }

    public class Vehicle
    {
        public VehicleType Type { get; set; }

        public double AntennaHeight { get; set; }
        public double AntennaPivot { get; set; }
        public double AntennaOffset { get; set; }
    }
}
