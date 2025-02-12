using AgOpenGPS.Core.Drawing;
using AgOpenGPS.Properties;
using System.Drawing;

namespace AgOpenGPS.Classes
{
    public class VehicleTextures
    {
        private Texture2D _frontWheel;
        private Texture2D _tire;
        private Texture2D _toolAxle;

        public VehicleTextures()
        {
        }

        public Texture2D FrontWheel
        {
            get
            {
                if (_frontWheel == null) _frontWheel = new Texture2D(Resources.z_FrontWheels);
                return _frontWheel;
            }
        }

        public Texture2D Tire
        {
            get
            {
                if (_tire == null) _tire = new Texture2D(Resources.z_Tire);
                return _tire;
            }
        }

        public Texture2D ToolAxle
        {
            get
            {
                if (_toolAxle == null) _toolAxle = new Texture2D(Resources.z_Tool);
                return _toolAxle;
            }
        }

    }
}
