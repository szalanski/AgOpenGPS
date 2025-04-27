using AgLibrary.ViewModels;
using AgOpenGPS.Core.Presenters;
using System.Windows.Input;

namespace AgOpenGPS.Core.ViewModels
{
    public class ApplicationViewModel : DayNightAndUnitsViewModel
    {
        private readonly ApplicationModel _appModel;
        private ApplicationPresenter _applicationPresenter;
        private StartNewFieldViewModel _startNewFieldViewModel;

        public ApplicationViewModel(ApplicationModel appModel)
        {
            _appModel = appModel;
            StartNewFieldCommand = new RelayCommand(StartNewField);
        }

        public void SetPresenter(ApplicationPresenter appPresenter)
        {
            _applicationPresenter = appPresenter;
        }

        public StartNewFieldViewModel StartNewFieldViewModel
        {
            get
            {
                if (_startNewFieldViewModel == null)
                {
                    _startNewFieldViewModel =
                        new StartNewFieldViewModel(
                            _appModel,
                            _applicationPresenter.PanelPresenter.NewFieldPanelPresenter);
                }
                return _startNewFieldViewModel;
            }
        }

        public ICommand StartNewFieldCommand { get; }
        private void StartNewField()
        {
            _applicationPresenter.PresentStartNewField(StartNewFieldViewModel);
        }
    }
}
