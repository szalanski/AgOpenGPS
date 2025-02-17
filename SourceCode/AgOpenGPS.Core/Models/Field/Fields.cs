using AgOpenGPS.Core.Streamers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace AgOpenGPS.Core.Models
{
    public class Fields
    {
        private readonly string _fieldsDirectory;
        private Field _currentField;
        private FieldStreamer _fieldStreamer;

        public Fields(string fieldsDirectory)
        {
            _fieldsDirectory = fieldsDirectory;
        }

        public Field CurrentField
        {
            get { return _currentField; }
            private set
            {
                _currentField = value;
                FieldStreamer = new FieldStreamer(CurrentField);
            }
        }

        public FieldStreamer FieldStreamer
        {
            get { return _fieldStreamer; }
            private set {
                _fieldStreamer = value;
            }
        }

        public ReadOnlyCollection<FieldDescription> GetFieldDescriptions()
        {
            string[] dirs = Directory.GetDirectories(_fieldsDirectory);

            if (dirs == null || dirs.Length < 1)
            {
                return null;
            }
            List<FieldDescription> list = new List<FieldDescription>();
            BoundaryStreamer boundaryStreamer = new BoundaryStreamer();
            OverviewStreamer overviewStreamer = new OverviewStreamer();

            foreach (string dir in dirs)
            {
                string fieldName = Path.GetFileName(dir);
                string filename = dir + "\\Field.txt";

                if (File.Exists(filename))
                {
                    var fieldDescription = new FieldDescription(fieldName);
                    var boundary = boundaryStreamer.Read(dir);
                    fieldDescription.Area = boundary.Area;

                    var overview = overviewStreamer.Read(dir);
                    fieldDescription.Wgs84Start = overview.Start;
                    list.Add(fieldDescription);
                }
            }
            return list.AsReadOnly();
        }

        public void DeleteField(string fieldName)
        {
            string dir = _fieldsDirectory + "\\" + fieldName;
            System.IO.Directory.Delete(dir);
        }

        public void SelectField(string fieldName)
        {
            CurrentField = new Field(fieldName);
        }

    }
}
