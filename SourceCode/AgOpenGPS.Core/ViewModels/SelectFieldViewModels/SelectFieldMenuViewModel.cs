using AgLibrary.ViewModels;
using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Streamers;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace AgOpenGPS.Core.ViewModels
{
    public class SelectFieldMenuViewModel : DayNightAndUnitsViewModel
    {
        private readonly ApplicationModel _appModel;
        private readonly ISelectFieldPanelPresenter _newFieldPanelPresenter;
        private SelectNearFieldViewModel _selectNearFieldViewModel;
        private CreateFromExistingFieldViewModel _createFromExistingFieldViewModel;
        private SelectFieldViewModel _selectFieldViewModel;

        public SelectFieldMenuViewModel(
            ApplicationModel appModel,
            ISelectFieldPanelPresenter newFieldPanelPresenter)
        {
            _appModel = appModel;
            _newFieldPanelPresenter = newFieldPanelPresenter;
            StartSelectNearFieldCommand = new RelayCommand(StartSelectNearField);
            StartCreateFieldFromExistingCommand = new RelayCommand(StartCreateFieldFromExisting);
            StartSelectFieldCommand = new RelayCommand(StartSelectField);
            CancelCommand = new RelayCommand(Cancel);
            _newFieldPanelPresenter = newFieldPanelPresenter;
        }

        public SelectNearFieldViewModel SelectNearFieldViewModel
        {
            get
            {
                if (_selectNearFieldViewModel == null)
                {
                    _selectNearFieldViewModel =
                        new SelectNearFieldViewModel(_appModel, _newFieldPanelPresenter);
                    AddChild(_selectNearFieldViewModel);
                }
                return _selectNearFieldViewModel;
            }
        }

        public CreateFromExistingFieldViewModel CreateFromExistingFieldViewModel
        {
            get
            {
                if (_createFromExistingFieldViewModel == null)
                {
                    _createFromExistingFieldViewModel =
                        new CreateFromExistingFieldViewModel(_appModel, _newFieldPanelPresenter);
                    AddChild(_createFromExistingFieldViewModel);
                }
                return _createFromExistingFieldViewModel;
            }
        }

        public SelectFieldViewModel SelectFieldViewModel
        {
            get
            {
                if (_selectFieldViewModel == null)
                {
                    _selectFieldViewModel = new SelectFieldViewModel(_appModel, _newFieldPanelPresenter);
                    AddChild(_selectFieldViewModel);
                }
                return _selectFieldViewModel;
            }
        }

        public ICommand StartSelectNearFieldCommand { get;}
        public ICommand StartCreateFieldFromExistingCommand { get; }
        public ICommand StartSelectFieldCommand { get; }
        public ICommand CancelCommand { get; }

        public string CurrentFieldName => _appModel.Fields.CurrentFieldName;

        private void StartSelectNearField()
        {
            // TODO implement different behaviour if number of fields is 0 or 1
            _newFieldPanelPresenter.CloseSelectFieldMenuDialog();
            SelectNearFieldViewModel.UpdateFields();
            _newFieldPanelPresenter.ShowSelectNearFieldDialog(SelectNearFieldViewModel);
        }

        private void StartCreateFieldFromExisting()
        {
            // TODO implement different behaviour if number of fields is 0 or 1
            _newFieldPanelPresenter.CloseSelectFieldMenuDialog();
            CreateFromExistingFieldViewModel.UpdateFields();
            _newFieldPanelPresenter.ShowCreateFromExistingFieldDialog(CreateFromExistingFieldViewModel);
        }

        private void StartSelectField()
        {
            // TODO implement different behaviour if number of fields is 0 or 1
            _newFieldPanelPresenter.CloseSelectFieldMenuDialog();
            SelectFieldViewModel.UpdateFields();
            _newFieldPanelPresenter.ShowSelectFieldDialog(SelectFieldViewModel);
        }

        private void Cancel()
        {
            _newFieldPanelPresenter.CloseSelectFieldMenuDialog();
        }

    }
}
