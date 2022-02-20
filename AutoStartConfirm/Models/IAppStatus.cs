using System.ComponentModel;

namespace AutoStartConfirm.Models
{
    public interface IAppStatus
    {
        bool HasOwnAutoStart { get; set; }
        int RunningActionCount { get; }

        event PropertyChangedEventHandler PropertyChanged;

        void DecrementRunningActionCount();
        void IncrementRunningActionCount();
    }
}