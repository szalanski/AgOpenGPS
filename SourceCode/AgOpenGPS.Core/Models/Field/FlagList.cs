using System.Collections.Generic;

namespace AgOpenGPS.Core.Models
{
    public class FlagList
    {
        private readonly List<Flag> _flags;
        public FlagList()
        {
            _flags = new List<Flag>();
        }

        public List<Flag> List => _flags;
    }
}
