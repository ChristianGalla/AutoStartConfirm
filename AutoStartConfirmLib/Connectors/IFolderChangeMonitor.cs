using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors
{
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