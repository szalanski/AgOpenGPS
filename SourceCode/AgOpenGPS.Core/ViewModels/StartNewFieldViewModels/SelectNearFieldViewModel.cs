using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Streamers;

namespace AgOpenGPS.Core.ViewModels
{
    public class SelectNearFieldViewModel : NearFieldTableViewModel
    {
        private readonly INewFieldPanelPresenter _newFieldPanelPresenter;

        public SelectNearFieldViewModel(
            ApplicationModel appModel,
            FieldDescriptionStreamer fieldDescriptionStreamer,
            FieldStreamer fieldStreamer,
            INewFieldPanelPresenter newFieldPanelPresenter
        )
            : base(appModel, fieldDescriptionStreamer, fieldStreamer)
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
