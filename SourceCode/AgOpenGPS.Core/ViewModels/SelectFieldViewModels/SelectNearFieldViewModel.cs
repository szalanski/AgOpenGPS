using AgOpenGPS.Core.Interfaces;

namespace AgOpenGPS.Core.ViewModels
{
    public class SelectNearFieldViewModel : NearFieldTableViewModel
    {
        private readonly ISelectFieldPanelPresenter _newFieldPanelPresenter;

        public SelectNearFieldViewModel(
            ApplicationModel appModel,
            ISelectFieldPanelPresenter newFieldPanelPresenter
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
