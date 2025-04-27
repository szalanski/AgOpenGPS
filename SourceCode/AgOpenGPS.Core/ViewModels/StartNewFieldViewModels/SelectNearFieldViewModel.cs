using AgOpenGPS.Core.Interfaces;

namespace AgOpenGPS.Core.ViewModels
{
    public class SelectNearFieldViewModel : NearFieldTableViewModel
    {
        private readonly INewFieldPanelPresenter _newFieldPanelPresenter;

        public SelectNearFieldViewModel(
            ApplicationModel appModel,
            INewFieldPanelPresenter newFieldPanelPresenter
        )
            : base(appModel)
        {
            _newFieldPanelPresenter = newFieldPanelPresenter;
        }

        protected override void SelectField()
        {
            _newFieldPanelPresenter.CloseSelectNearFieldDialog();
            base.SelectField();
        }
    }
}
