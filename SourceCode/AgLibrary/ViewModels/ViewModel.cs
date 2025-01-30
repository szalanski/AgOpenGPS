using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AgLibrary.ViewModels
{
    //public class DayNightViewModel : ViewModel
    //{
    //    private bool _isDay;
    //    public DayNightViewModel()
    //    {
    //    }

    //    public bool IsDay
    //    {
    //        get { return _isDay; }
    //        set
    //        {
    //            if (value != _isDay)
    //            {
    //                _isDay = value;
    //                NotifyPropertyChanged();
    //            }
    //        }
    //    }
    //}

    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void NotifyAllPropertiesChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        }
    }
}
