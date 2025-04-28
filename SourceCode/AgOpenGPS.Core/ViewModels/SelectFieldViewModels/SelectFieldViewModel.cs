using AgLibrary.ViewModels;
using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Models;
using System.Windows.Input;

namespace AgOpenGPS.Core.ViewModels
{
    public class SelectFieldViewModel : FieldTableViewModel
    {
        private readonly ISelectFieldPanelPresenter _newFieldPanelPresenter;
        public SelectFieldViewModel(
            ApplicationModel appModel,
            ISelectFieldPanelPresenter newFieldPanelPresenter
        )
            : base(appModel)
        {
            _newFieldPanelPresenter = newFieldPanelPresenter;
            DeleteFieldCommand = new RelayCommand(DeleteField);
        }

        public ICommand DeleteFieldCommand { get; }

        private void DeleteField()
        {
            var selectedField = LocalSelectedField;
            if (null != selectedField)
            {
                if (_newFieldPanelPresenter.ShowConfirmDeleteMessageBox(selectedField.FieldName))
                {
                    _appModel.Fields.DeleteField(selectedField.DirectoryInfo);
                    LocalSelectedField = null;
                    UpdateFields();
                }
            }
        }

        protected override void SelectField()
        {
            _newFieldPanelPresenter.CloseSelectFieldDialog();
            base.SelectField();
        }

    }
}
