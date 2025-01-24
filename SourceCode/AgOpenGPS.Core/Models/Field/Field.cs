using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Core.Models
{
    public class Field
    {
        public Field()
        {
        }

        public Boundary Boundary { get; set; }
        public FlagList Flags { get; set; }
        public RecordedPath RecordedPath { get; set; }
        public TramLines TramLines { get; set; }

    }
}
