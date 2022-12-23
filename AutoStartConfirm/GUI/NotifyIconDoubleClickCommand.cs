using AutoStartConfirm.Connectors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;

namespace AutoStartConfirm.GUI
{
    /// <summary>
    /// Show main window on double click in nofifyicon
    /// </summary>
    public class NotifyIconDoubleClickCommand : ICommand, IDisposable
    {
        public void Execute(object parameter)
        {
            AppInstance.ToggleMainWindow();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        private bool disposedValue;

        private readonly IServiceScope ServiceScope;

        public readonly IApp AppInstance;

        public NotifyIconDoubleClickCommand()
        {
            ServiceScope = App.ServiceProvider.CreateScope();
            AppInstance = ServiceScope.ServiceProvider.GetRequiredService<IApp>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceScope.Dispose();
                }

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ConverterBase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
