using AgOpenGPS.Core.Interfaces;
using System.IO;

namespace AgOpenGPS.Core.Streamers
{
    public abstract class FieldAspectStreamer
    {
        protected readonly ILogger _logger;
        protected readonly string _defaultFileName;

        protected IFieldStreamerPresenter _presenter;

        public FieldAspectStreamer(
            ILogger logger,
            string defaultFileName)
        {
            _logger = logger;
            _defaultFileName = defaultFileName;
        }

        public void SetPresenter(IFieldStreamerPresenter presenter)
        {
            _presenter = presenter;
        }

        public string FullPath(string fieldPath, string fileName = null)
        {
            string fn = fileName ?? _defaultFileName;
            return fieldPath + "\\" + fn;
        }

        public void CreateDirectory(string fieldPath)
        {
            string dirField = fieldPath + "\\";

            string directoryName = Path.GetDirectoryName(dirField);
            if (0 < directoryName.Length && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }
    }
}
