using AgLibrary.ViewModels;
using System.Collections.ObjectModel;

namespace AgOpenGPS.Core.ViewModels
{
    // The class DayNightAndUnitsViewModel implements the properties IsDay and IsMetric.
    // Moreover, an instance of this class propagates changes to these properites to its childs.
    public class DayNightAndUnitsViewModel : ViewModel
    {
        private readonly Collection<DayNightAndUnitsViewModel> _children;
        private bool _isMetric;
        private bool _isDay;

        public DayNightAndUnitsViewModel()
        {
            _children = new Collection<DayNightAndUnitsViewModel>();
        }

        public bool IsMetric
        {
            get { return _isMetric; }
            set
            {
                if (value != _isMetric)
                {
                    _isMetric = value;
                    NotifyAllPropertiesChanged();
                }
            }
        }

        public bool IsDay
        {
            get { return _isMetric; }
            set
            {
                if (value != _isMetric)
                {
                    _isMetric = value;
                    NotifyAllPropertiesChanged();
                }
            }
        }

        protected void AddChild(DayNightAndUnitsViewModel child)
        {
            _children.Add(child);
            child.IsDay = IsDay;
            child.IsMetric = IsMetric;
        }
    }

}
