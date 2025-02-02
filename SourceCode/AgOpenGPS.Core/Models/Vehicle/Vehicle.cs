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
    }
}
