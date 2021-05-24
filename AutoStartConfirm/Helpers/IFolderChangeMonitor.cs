using AutoStartConfirm.AutoStarts;
using AutoStartConfirm.Connectors;

namespace AutoStartConfirm.Helpers {
    public interface IFolderChangeMonitor {
        string BasePath { get; set; }
        Category Category { get; set; }

        event AutoStartChangeHandler Add;
        event AutoStartChangeHandler Remove;

        void Dispose();
        void Start();
        void Stop();
    }
}