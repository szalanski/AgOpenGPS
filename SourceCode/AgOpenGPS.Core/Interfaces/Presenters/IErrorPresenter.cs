namespace AgOpenGPS.Core.Interfaces
{
    public interface IErrorPresenter
    {
        void PresentTimedMessage(
            int timeInMilliSec,
            string titleString,
            string messageString);
    }
}
