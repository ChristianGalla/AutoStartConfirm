using System.ComponentModel;

namespace AutoStartConfirm.Models
{
    public interface IAppStatus: INotifyPropertyChanged
    {
        bool HasOwnAutoStart { get; set; }
        int RunningActionCount { get; }

        void DecrementRunningActionCount();
        void IncrementRunningActionCount();
    }
}