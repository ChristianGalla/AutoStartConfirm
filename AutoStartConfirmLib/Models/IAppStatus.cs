using System.ComponentModel;

namespace AutoStartConfirm.Models
{
    public interface IAppStatus : INotifyPropertyChanged
    {
        bool HasOwnAutoStart { get; set; }
        bool IsOwnAutoStartToggling { get; set; }
        bool IsNotOwnAutoStartToggling { get; set; }
        int RunningActionCount { get; }

        void DecrementRunningActionCount();
        void IncrementRunningActionCount();
    }
}