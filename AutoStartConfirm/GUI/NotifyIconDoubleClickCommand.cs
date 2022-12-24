using AutoStartConfirm.Connectors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace AutoStartConfirm.GUI
{
    public delegate void DoubleClickHandler();

    /// <summary>
    /// Show main window on double click in nofifyicon
    /// </summary>
    public class NotifyIconDoubleClickCommand : ICommand
    {
        public void Execute(object parameter)
        {
            DoubleClick?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public event EventHandler DoubleClick;
    }
}
