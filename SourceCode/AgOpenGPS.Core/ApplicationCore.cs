using AgOpenGPS.Core.Models;

namespace AgOpenGPS.Core
{
    public class ApplicationCore
    {

        public ApplicationCore(string baseDirectory)
        {
            AppModel = new ApplicationModel(baseDirectory);
        }

        public ApplicationModel AppModel { get; }

        public Field CurrentField => AppModel.Fields.CurrentField;

    }
}
