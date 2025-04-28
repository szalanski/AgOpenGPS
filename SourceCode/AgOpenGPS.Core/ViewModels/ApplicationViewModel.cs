﻿using AgLibrary.ViewModels;
using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Presenters;
using System.Windows.Input;

namespace AgOpenGPS.Core.ViewModels
{
    public class ApplicationViewModel : DayNightAndUnitsViewModel
    {
        private readonly ApplicationModel _appModel;
        private ApplicationPresenter _applicationPresenter;
        private ConfigMenuViewModel _configMenuViewModel;
        private SelectFieldMenuViewModel _selectFieldMenuViewModel;

        public ApplicationViewModel(ApplicationModel appModel)
        {
            _appModel = appModel;
            ShowConfigMenuCommand = new RelayCommand(ShowConfigMenu);
            ShowSelectFieldMenuCommand = new RelayCommand(ShowSelectFieldMenu);
        }

        public IPanelPresenter PanelPresenter => _applicationPresenter.PanelPresenter;

        public void SetPresenter(ApplicationPresenter appPresenter)
        {
            _applicationPresenter = appPresenter;
        }

        public ICommand ShowConfigMenuCommand { get; }
        public ICommand ShowSelectFieldMenuCommand { get; }

        public ConfigMenuViewModel ConfigMenuViewModel
        {
            get
            {
                if (_configMenuViewModel == null)
                {
                    _configMenuViewModel =
                        new ConfigMenuViewModel(
                            _appModel,
                            _applicationPresenter.PanelPresenter.ConfigMenuPanelPresenter);
                    AddChild(_configMenuViewModel);
                }
                return _configMenuViewModel;
            }
        }

        public SelectFieldMenuViewModel SelectFieldMenuViewModel
        {
            get
            {
                if (_selectFieldMenuViewModel == null)
                {
                    _selectFieldMenuViewModel =
                        new SelectFieldMenuViewModel(
                            _appModel,
                            _applicationPresenter.PanelPresenter.SelectFieldPanelPresenter);
                    AddChild(_selectFieldMenuViewModel);
                }
                return _selectFieldMenuViewModel;
            }
        }

        private void ShowConfigMenu()
        {
            PanelPresenter.ConfigMenuPanelPresenter.ShowConfigMenuDialog(ConfigMenuViewModel);
        }

        private void ShowSelectFieldMenu()
        {
            PanelPresenter.SelectFieldPanelPresenter.ShowSelectFieldMenuDialog(SelectFieldMenuViewModel);
        }

    }
}
