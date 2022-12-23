using AutoStartConfirm.Connectors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace AutoStartConfirm.Models
{
    public class NotifyIconViewModel
    {
        private bool disposedValue;

        private readonly IServiceScope ServiceScope;

        public readonly IAppStatus AppStatus;

        public NotifyIconViewModel()
        {
            ServiceScope = App.ServiceProvider.CreateScope();
            AppStatus = ServiceScope.ServiceProvider.GetRequiredService<IAppStatus>();
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
