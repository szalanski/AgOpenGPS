using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Core.Streamers
{
    public class FieldStreamer
    {
        private readonly Field _field;

        private readonly BoundaryStreamer _boundaryStreamer;
        private readonly ContourStreamer _contourStreamer;
        private readonly FlagListStreamer _flagsStreamer;
        private readonly RecordedPathStreamer _recordedPathStreamer;
        private readonly TramLinesStreamer _tramLinesStreamer;
        private readonly WorkedAreaStreamer _workedAreaStreamer;

        private IFieldStreamerPresenter _presenter;

        public FieldStreamer(Field field)
        {
            _field = field;
            _boundaryStreamer = new BoundaryStreamer();
            _contourStreamer = new ContourStreamer();
            _flagsStreamer = new FlagListStreamer();
            _recordedPathStreamer = new RecordedPathStreamer();
            _tramLinesStreamer = new TramLinesStreamer();
            _workedAreaStreamer = new WorkedAreaStreamer();
        }

        public void SetPresenter(IFieldStreamerPresenter presenter)
        {
            _presenter = presenter;
            _boundaryStreamer.SetPresenter(presenter);
            _contourStreamer.SetPresenter(presenter);
            _flagsStreamer.SetPresenter(presenter);
            _recordedPathStreamer.SetPresenter(presenter);
            _tramLinesStreamer.SetPresenter(presenter);
            _workedAreaStreamer.SetPresenter(presenter);
        }

        public string CurrentFieldPath { get; set; }

        public Boundary ReadBoundary(string fieldPath)
        {
            return _boundaryStreamer.Read(fieldPath);
        }

        public void ReadBoundary()
        {
            _field.Boundary = _boundaryStreamer.Read(CurrentFieldPath);
        }

        public void WriteBoundary()
        {
            _boundaryStreamer.Write(_field.Boundary, CurrentFieldPath);
        }

        public void ReadFlagList()
        {
            _field.Flags = _flagsStreamer.TryRead(CurrentFieldPath);
        }

        public void WriteFlagList()
        {
            _flagsStreamer.TryWrite(_field.Flags, CurrentFieldPath);
        }

        public void ReadContour()
        {
            _field.Contour = _contourStreamer.TryRead(CurrentFieldPath);
        }

        public void ContourAppendUnsavedWork()
        {
            _contourStreamer.AppendUnsavedWork(_field.Contour, CurrentFieldPath);
        }

        public void ReadRecordedPath(string fileName = null)
        {
            _field.RecordedPath = _recordedPathStreamer.TryRead(CurrentFieldPath, fileName);
        }

        public void WriteRecordedPath(string fileName = null)
        {
            _recordedPathStreamer.Write(_field.RecordedPath, CurrentFieldPath, fileName);
        }

        public void CreateRecordedPathFile()
        {
            _recordedPathStreamer.CreateFile(CurrentFieldPath);
        }

        public void ReadTramLines()
        {
            _field.TramLines = _tramLinesStreamer.TryRead(CurrentFieldPath);
        }

        public void WriteTramLines()
        {
            _tramLinesStreamer.Write(_field.TramLines, CurrentFieldPath);
        }

        public void ReadWorkedAera()
        {
            _field.WorkedArea = _workedAreaStreamer.Read(CurrentFieldPath);
        }

        public void WorkedAreaAppendUnsaveWork()
        {
            _workedAreaStreamer.AppendUnsavedWork(_field.WorkedArea, CurrentFieldPath);
        }
    }
}
