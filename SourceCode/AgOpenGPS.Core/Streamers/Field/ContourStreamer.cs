using AgLibrary.Logging;
using AgOpenGPS.Core.Models;
using System.IO;

namespace AgOpenGPS.Core.Streamers
{
    public class ContourStreamer : FieldAspectStreamer
    {
        public ContourStreamer() : base("Contour.txt")
        {
        }

        public Contour TryRead(string fieldPath)
        {
            Contour contour = null;
            if (!File.Exists(FullPath(fieldPath)))
            {
                _presenter.PresentSectionFileMissing();
            }
            try
            {
                contour = Read(fieldPath);
            }
            catch (System.Exception e)
            {
                _presenter.PresentSectionFileCorrupt();
                Log.EventWriter("Section file" + e.ToString());
            }
            return contour;
        }

        public Contour Read(string fieldPath)
        {
            Contour contour = new Contour();
            using (GeoStreamReader reader = new GeoStreamReader(FullPath(fieldPath)))
            {
                //read header
                while (!reader.EndOfStream)
                {
                    GeoPathWithHeading path = reader.ReadGeoPathWithHeading();
                    contour.Strips.Add(path);
                }
            }
            return contour;
        }

        public void AppendUnsavedWork(Contour contour, string fieldPath)
        {
            using (GeoStreamWriter writer = new GeoStreamWriter(FullPath(fieldPath), true))
            {
                
                foreach (var path in contour.UnsavedStrips)
                {
                    writer.WriteGeoPathWithHeading(path);
                }
            }
            contour.UnsavedStrips.Clear();
        }
    }
}
