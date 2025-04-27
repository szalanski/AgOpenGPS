using AgOpenGPS.Core.Interfaces;

namespace AgOpenGPS.WpfApp.Presenters
{
    public class WpfPanelPresenter : IPanelPresenter
    {
        public WpfPanelPresenter()
        {
            NewFieldPanelPresenter = new WpfNewFieldPanelPresenter();
        }

        public INewFieldPanelPresenter NewFieldPanelPresenter {get;}
    }

}
