using Hardcodet.Wpf.TaskbarNotification;
using System;

namespace AutoStartConfirm.GUI
{
    public interface INotifyIcon
    {
        event EventHandler Exit;
        event EventHandler OwnAutoStartToggle;
        event EventHandler Open;

        void InitializeComponent();
    }
}