using AgOpenGPS.Core.ViewModels;

namespace AgOpenGPS.Core.Interfaces
{
    public interface IPanelPresenter
    {
        INewFieldPanelPresenter NewFieldPanelPresenter { get; }
        IConfigMenuPanelPresenter ConfigMenuPanelPresenter { get; }
    }
}
