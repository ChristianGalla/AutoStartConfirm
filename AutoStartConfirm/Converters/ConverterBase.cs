using AutoStartConfirm.Connectors;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace AutoStartConfirm.Converters {
    public class ConverterBase: IDisposable {
        private bool disposedValue;

        private readonly IServiceScope ServiceScope;

        public readonly IAutoStartService AutoStartService;

        public ConverterBase()
        {
            ServiceScope = App.ServiceProvider.CreateScope();
            AutoStartService = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
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