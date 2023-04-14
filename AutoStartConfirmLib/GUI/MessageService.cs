using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoStartConfirm.GUI
{
    public class MessageService : IMessageService, IDisposable {

        private readonly ILogger<MessageService> Logger;

        private bool disposedValue;

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        private readonly IDispatchService DispatchService;

        private MainWindow? window = null;

        private MainWindow Window
        {
            get
            {
                if (window == null)
                {
                    window = ServiceScope.ServiceProvider.GetRequiredService<MainWindow>();
                }
                return window;
            }
        }

        private readonly IAppStatus AppStatus;

        private readonly SemaphoreSlim dialogSemaphore = new(1, 1);

        // only one dialog is allowed at the same time
        public SemaphoreSlim DialogSemaphore {
            get => dialogSemaphore;
        }

        public MessageService(
            ILogger<MessageService> logger,
            IAppStatus appStatus,
            IDispatchService dispatchService)
        {
            Logger = logger;
            AppStatus = appStatus;
            DispatchService = dispatchService;
        }

        public async Task ShowError(string caption, Exception error) {
            await ShowError(caption, error.Message);
        }

        public async Task ShowError(string caption, string message = "")
        {
            Logger.LogTrace("Showing error dialog {caption}: {message}", caption, message);
            var tcs = new TaskCompletionSource();
            var queued = DispatchService.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
            {
                try
                {
                    ContentDialog dialog = new()
                    {
                        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                        XamlRoot = Window.Content.XamlRoot,

                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = caption,
                        CloseButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Close,
                        Content = message
                    };

                    await DialogSemaphore.WaitAsync();
                    try
                    {
                        var result = await dialog.ShowAsync();
                        Logger.LogTrace("Closed error dialog {caption}: {message}", caption, message);
                        if (tcs.TrySetResult())
                        {
                            Logger.LogError("Failed to set result");
                        }
                    }
                    finally
                    {
                        DialogSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to show error dialog");
                    tcs.TrySetResult();
                }
            });
            if (!queued)
            {
                Logger.LogError("Failed to enqueue error dialog in main thread");
                throw new Exception("Failed to enqueue error dialog in main thread");
            }
            await tcs.Task;
        }

        public async Task<bool> ShowConfirm(AutoStartEntry autoStart, string action)
        {
            return await ShowConfirm(
                $"Are you sure you want to {action} this auto start?",
                @$"Type:
{autoStart.Category}

Value:
{autoStart.Value}

Path:
{autoStart.Path}");
        }

        public async Task<bool> ShowConfirm(string caption, string message = "")
        {
            Logger.LogTrace("Showing confirm dialog {caption}: {message}", caption, message);
            var tcs = new TaskCompletionSource<bool>();
            var queued = DispatchService.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
            {
                try
                {
                    ContentDialog dialog = new()
                    {
                        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                        XamlRoot = Window.Content.XamlRoot,

                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = caption,
                        PrimaryButtonText = "Yes",
                        CloseButtonText = "No",
                        DefaultButton = ContentDialogButton.Close,
                        Content = message
                    };

                    await DialogSemaphore.WaitAsync();
                    try
                    {
                        var result = await dialog.ShowAsync();
                        Logger.LogTrace("Closed confirm dialog {caption}: {message}, Result: {result}", caption, message, result);
                        if (tcs.TrySetResult(result == ContentDialogResult.Primary))
                        {
                            Logger.LogError("Failed to set result");
                        }
                    }
                    finally
                    {
                        DialogSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to show confirm dialog");
                    tcs.TrySetResult(false);
                }

            });
            if (!queued)
            {
                Logger.LogError("Failed to enqueue confirm dialog in main thread");
                throw new Exception("Failed to enqueue confirm dialog in main thread");
            }
            return await tcs.Task;
        }

        public async Task ShowSuccess(string caption, string message = "")
        {
            Logger.LogTrace("Showing success dialog {caption}: {message}", caption, message);
            var tcs = new TaskCompletionSource();
            var queued = DispatchService.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
            {
                try
                {
                    ContentDialog dialog = new()
                    {
                        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                        XamlRoot = Window.Content.XamlRoot,

                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = caption,
                        CloseButtonText = "Ok",
                        DefaultButton = ContentDialogButton.Close,
                        Content = message
                    };

                    await DialogSemaphore.WaitAsync();
                    try
                    {
                        var result = await dialog.ShowAsync();
                        Logger.LogTrace("Closed success dialog {caption}: {message}", caption, message);
                        if (tcs.TrySetResult())
                        {
                            Logger.LogError("Failed to set result");
                        }
                    }
                    finally
                    {
                        DialogSemaphore.Release();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to show success dialog");
                    tcs.TrySetResult();
                }
            });
            if (!queued)
            {
                Logger.LogError("Failed to enqueue success dialog in main thread");
                throw new Exception("Failed to enqueue success dialog in main thread");
            }
            await tcs.Task;
        }


        /// <summary>
        /// Ensures that the main window is open.
        /// A new hidden window is created if it not already exists.
        /// </summary>
        /// <returns>true if a new window has been created</returns>
        private bool EnsureMainWindow() {
            Logger.LogTrace("Showing main window");
            bool newCreated = false;
            // MainWindow.Show();
            return newCreated;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    ServiceScope.Dispose();
                    DialogSemaphore.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MessageService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
