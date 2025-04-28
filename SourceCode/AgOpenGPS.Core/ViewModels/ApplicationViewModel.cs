using AgLibrary.ViewModels;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Presenters;
using AgOpenGPS.Core.Streamers;
using System.Windows.Input;

namespace AgOpenGPS.Core.ViewModels
{
    public class ApplicationViewModel : DayNightAndUnitsViewModel
    {
        private readonly ApplicationModel _applicationModel;
        private readonly FieldDescriptionStreamer _fieldDescriptionStreamer;
        private readonly FieldStreamer _fieldStreamer;
        private ApplicationPresenter _applicationPresenter;
        private StartNewFieldViewModel _startNewFieldViewModel;

        public ApplicationViewModel(
            ApplicationModel applicationModel,
            FieldDescriptionStreamer fieldDescriptionStreamer,
            FieldStreamer fieldStreamer)
        {
            _applicationModel = applicationModel;
            _fieldDescriptionStreamer = fieldDescriptionStreamer;
            _fieldStreamer = fieldStreamer;
            StartNewFieldCommand = new RelayCommand(StartNewField);
        }

        public void SetPresenter(ApplicationPresenter appPresenter)
        {
            _applicationPresenter = appPresenter;
        }

        public ICommand StartNewFieldCommand { get; }

        public StartNewFieldViewModel StartNewFieldViewModel
        {
            get
            {
                if (_startNewFieldViewModel == null)
                {
                    _startNewFieldViewModel =
                        new StartNewFieldViewModel(
                            _applicationModel,
                            _fieldDescriptionStreamer,
                            _fieldStreamer,
                            _applicationPresenter.PanelPresenter.NewFieldPanelPresenter);
                }
                return _startNewFieldViewModel;
            }
        }

        private void StartNewField()
        {
            _applicationPresenter.PresentStartNewField(StartNewFieldViewModel);
        }
    }
}
