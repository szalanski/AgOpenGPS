﻿using AgLibrary.ViewModels;
using AgOpenGPS.Core.Presenters;
using System.Windows.Input;

namespace AgOpenGPS.Core.ViewModels
{
    public class ApplicationViewModel : DayNightAndUnitsViewModel
    {
        private readonly ApplicationModel _appModel;
        private ApplicationPresenter _applicationPresenter;
        private ConfigMenuViewModel _configMenuViewModel;
        private StartNewFieldViewModel _startNewFieldViewModel;

        public ApplicationViewModel(ApplicationModel appModel)
        {
            _appModel = appModel;
            ShowConfigMenuCommand = new RelayCommand(ShowConfigMenu);
            StartNewFieldCommand = new RelayCommand(StartNewField);
        }

        public void SetPresenter(ApplicationPresenter appPresenter)
        {
            _applicationPresenter = appPresenter;
        }

        public ICommand ShowConfigMenuCommand { get; }
        public ICommand StartNewFieldCommand { get; }

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

        private void ShowConfigMenu()
        {
            _applicationPresenter.PanelPresenter.ConfigMenuPanelPresenter.ShowConfigMenuDialog(ConfigMenuViewModel);
        }

        private void StartNewField()
        {
            _applicationPresenter.PresentStartNewField(StartNewFieldViewModel);
        }

    }
}
