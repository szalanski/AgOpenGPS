using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Streamers;

namespace AgOpenGPS.Core.ViewModels
{
    public class SelectNearFieldViewModel : NearFieldTableViewModel
    {
        private readonly IPanelPresenter _panelPresenter;

        public SelectNearFieldViewModel(
            ApplicationModel appModel,
            FieldDescriptionStreamer fieldDescriptionStreamer,
            FieldStreamer fieldStreamer,
            IPanelPresenter panelPresenter
        )
            : base(appModel, fieldDescriptionStreamer, fieldStreamer)
        {
            _panelPresenter = panelPresenter;
        }

        protected override void SelectField()
        {
            _panelPresenter.CloseSelectNearFieldDialog();
            base.SelectField();
        }
    }
}
