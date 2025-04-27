using AgOpenGPS.Core.Interfaces;

namespace AgOpenGPS.WpfApp.Presenters
{
    public class WpfPanelPresenter : IPanelPresenter
    {
        public WpfPanelPresenter()
        {
            ConfigMenuPanelPresenter = new WpfConfigMenuPanelPresenter();
            NewFieldPanelPresenter = new WpfNewFieldPanelPresenter();
        }

        public IConfigMenuPanelPresenter ConfigMenuPanelPresenter { get; }
        public INewFieldPanelPresenter NewFieldPanelPresenter { get; }
    }

}
