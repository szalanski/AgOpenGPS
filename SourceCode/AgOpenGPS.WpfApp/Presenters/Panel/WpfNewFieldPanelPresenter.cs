using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.ViewModels;
using AgOpenGPS.WpfApp.Field;
using System;
using System.Windows;

namespace AgOpenGPS.WpfApp
{
    public class WpfNewFieldPanelPresenter : INewFieldPanelPresenter
    {
        private StartNewFieldDialog _startNewFieldDialog;
        private SelectNearFieldDialog _selectNearFieldDialog;
        private CreateFromExistingFieldDialog _createFromExistingFieldDialog;
        private SelectFieldDialog _selectFieldDialog;

        void INewFieldPanelPresenter.ShowStartNewFieldDialog(StartNewFieldViewModel viewModel)
        {
            _startNewFieldDialog = new StartNewFieldDialog
            {
                DataContext = viewModel
            };
            _startNewFieldDialog.ShowDialog();
        }

        void INewFieldPanelPresenter.CloseStartNewFieldDialog()
        {
            _startNewFieldDialog?.Close();
        }

        void INewFieldPanelPresenter.ShowSelectNearFieldDialog(SelectNearFieldViewModel viewModel)
        {
            viewModel.UpdateFields();
            _selectNearFieldDialog = new SelectNearFieldDialog
            {
                DataContext = viewModel
            };
            _selectNearFieldDialog.ShowDialog();
        }

        void INewFieldPanelPresenter.CloseSelectNearFieldDialog()
        {
            _selectNearFieldDialog?.Close();
        }

        void INewFieldPanelPresenter.ShowCreateFromExistingFieldDialog(
            CreateFromExistingFieldViewModel viewModel)
        {
            viewModel.UpdateFields();
            _createFromExistingFieldDialog = new CreateFromExistingFieldDialog
            {
                DataContext = viewModel
            };
            _createFromExistingFieldDialog.ShowDialog();
        }

        void INewFieldPanelPresenter.CloseCreateFromExistingFieldDialog()
        {
            _createFromExistingFieldDialog?.Close();
        }

        void INewFieldPanelPresenter.ShowSelectFieldDialog(SelectFieldViewModel viewModel)
        {
            viewModel.UpdateFields();
            _selectFieldDialog = new SelectFieldDialog
            {
                DataContext = viewModel
            };
            _selectFieldDialog.ShowDialog();
        }

        void INewFieldPanelPresenter.CloseSelectFieldDialog()
        {
            _selectFieldDialog.Close();
        }

        bool INewFieldPanelPresenter.ShowConfirmDeleteMessageBox(string fieldName)
        {
            MessageBoxResult result = MessageBox.Show(
                "Are you sure you want to delete field " + fieldName + "?",
                "Delete field",
                MessageBoxButton.OKCancel);
            return result == MessageBoxResult.OK;
        }

    }
}
