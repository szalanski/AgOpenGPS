using System.IO;

namespace AgOpenGPS.Core.Models
{
    public class BaseDirectories
    {
        public BaseDirectories(string baseDirectory)
        {
            //get the fields directory, if not exist, create
            FieldsDir = baseDirectory + "Fields\\";
            string dir = Path.GetDirectoryName(FieldsDir);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

            //get the Vehices directory, if not exist, create
            VehiclesDir = baseDirectory + "Vehicles\\";
            dir = Path.GetDirectoryName(VehiclesDir);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
        }

        public string FieldsDir { get; }
        public string VehiclesDir { get; }

    }
}
