using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows;

namespace AutoStartConfirm.Converters {
    public class ConverterBase: IDisposable {
        private bool disposedValue;

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        public SortedList<string, ConnectorEnableRow> Connectors;

#pragma warning disable CA2213 // Disposable fields should be disposed
        // Disposed by ServiceProvider
        private ISettingsService settingsService;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public ISettingsService SettingsService
        {
            get
            {
                settingsService ??= ServiceScope.ServiceProvider.GetService<ISettingsService>();
                return settingsService;
            }
        }

        private IAutoStartService autoStartService;

        public IAutoStartService AutoStartService
        {
            get
            {
                autoStartService ??= ServiceScope.ServiceProvider.GetService<IAutoStartService>();
                return autoStartService;
            }
        }

        public ConverterBase()
        {
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