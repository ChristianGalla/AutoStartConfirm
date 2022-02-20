using System;
using System.Windows;
using System.Windows.Input;

namespace AutoStartConfirm.GUI
{
    /// <summary>
    /// Show main window on double click in nofifyicon
    /// </summary>
    public class NotifyIconDoubleClickCommand : ICommand
    {
        public readonly IBusiness _business;

        public NotifyIconDoubleClickCommand(IBusiness business)
        {
            _business = business;
        }

        public void Execute(object parameter)
        {
            _business.ToggleMainWindow();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67
    }
}
