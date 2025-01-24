using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Core.Streamers
{
    public class FieldStreamer
    {
        private readonly Field _field;

        private readonly BoundaryStreamer _boundaryStreamer;
        private readonly FlagListStreamer _flagsStreamer;
        private readonly RecordedPathStreamer _recordedPathStreamer;
        private readonly TramLinesStreamer _tramLinesStreamer;

        private IFieldStreamerPresenter _presenter;
        private string _currentFieldPath;

        public FieldStreamer(Field field, ILogger logger)
        {
            _field = field;
            _boundaryStreamer = new BoundaryStreamer(logger);
            _flagsStreamer = new FlagListStreamer(logger);
            _recordedPathStreamer = new RecordedPathStreamer(logger);
            _tramLinesStreamer = new TramLinesStreamer(logger);
        }

        public void SetPresenter(IFieldStreamerPresenter presenter)
        {
            _presenter = presenter;
            _flagsStreamer.SetPresenter(presenter);
            _recordedPathStreamer.SetPresenter(presenter);
            _tramLinesStreamer.SetPresenter(presenter);
        }

        public string CurrentFieldPath
        {
            set { _currentFieldPath = value; }
        }

        public Boundary ReadBoundary(string fieldPath)
        {
            return _boundaryStreamer.Read(fieldPath);
        }

        public void ReadBoundary()
        {
            _field.Boundary = _boundaryStreamer.Read(_currentFieldPath);
        }

        public void WriteBoundary()
        {
            _boundaryStreamer.Write(_field.Boundary, _currentFieldPath);
        }

        public void ReadFlagList()
        {
            _field.Flags = _flagsStreamer.TryRead(_currentFieldPath);
        }

        public void WriteFlagList()
        {
            _flagsStreamer.TryWrite(_field.Flags, _currentFieldPath);
        }

        public void ReadRecordedPath(string fileName = null)
        {
            _field.RecordedPath = _recordedPathStreamer.TryRead(_currentFieldPath, fileName);
        }

        public void WriteRecordedPath(string fileName = null)
        {
            _recordedPathStreamer.TryWrite(_field.RecordedPath, _currentFieldPath, fileName);
        }

        public void CreateRecordedPathFile()
        {
            _recordedPathStreamer.CreateFile(_currentFieldPath);
        }

        public void ReadTramLines()
        {
            _field.TramLines = _tramLinesStreamer.TryRead(_currentFieldPath);
        }

        public void WriteTramLines()
        {
            _tramLinesStreamer.Write(_field.TramLines, _currentFieldPath);
        }
    }
}
