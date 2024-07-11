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
using Windows.ApplicationModel.Resources;
using static AutoStartConfirm.GUI.IMessageService;

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

        private readonly SemaphoreSlim dialogSemaphore = new(1, 1);

        // only one dialog is allowed at the same time
        public SemaphoreSlim DialogSemaphore {
            get => dialogSemaphore;
        }

        private readonly ResourceLoader ResourceLoader = new("AutoStartConfirmLib/Resources");

        public MessageService(
            ILogger<MessageService> logger,
            IDispatchService dispatchService)
        {
            Logger = logger;
            DispatchService = dispatchService;
        }

        public async Task ShowError(string caption, Exception error) {
            await ShowError(caption, error.Message);
        }

        public async Task ShowError(string caption, string message = "")
        {
            Logger.LogTrace("Showing error dialog {caption}: {message}", caption, message);
            var tcs = new TaskCompletionSource();
            var queued = DispatchService.TryEnqueue(DispatcherQueuePriority.High, async () =>
            {
                try
                {
                    ContentDialog dialog = new()
                    {
                        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                        XamlRoot = Window.Content.XamlRoot,

                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = caption,
                        CloseButtonText = ResourceLoader.GetString($"Message/Button/Ok"),
                        DefaultButton = ContentDialogButton.Close,
                        Content = message
                    };

                    await DialogSemaphore.WaitAsync();
                    try
                    {
                        Window.AppWindow.Show();
                        var result = await dialog.ShowAsync();
                        Logger.LogTrace("Closed error dialog {caption}: {message}", caption, message);
                        tcs.TrySetResult();
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

        public async Task<bool> ShowConfirm(AutoStartEntry autoStart, AutoStartAction action)
        {
            return await ShowConfirm(autoStart.Category, autoStart.Value, autoStart.Path, action);
        }

        public async Task<bool> ShowConfirm(IgnoredAutoStart autoStart)
        {
            return await ShowConfirm(autoStart.Category, autoStart.Value, autoStart.Path, AutoStartAction.RemoveIgnore);
        }

        public async Task<bool> ShowConfirm(Category autoStartCategory, string autoStartValue, string autoStartPath, AutoStartAction action)
        {
            var title = ResourceLoader.GetString($"MessageConfirm/Title/{action}");
            var type = ResourceLoader.GetString("Message/Type");
            var value = ResourceLoader.GetString("Message/Value");
            var path = ResourceLoader.GetString("Message/Path");
            return await ShowConfirm(
                title,
                @$"{type}:
{autoStartCategory}

{value}:
{autoStartValue}

{path}:
{autoStartPath}");
        }

        public async Task<bool> ShowConfirm(string caption, string message = "")
        {
            Logger.LogTrace("Showing confirm dialog {caption}: {message}", caption, message);
            var tcs = new TaskCompletionSource<bool>();
            var queued = DispatchService.TryEnqueue(DispatcherQueuePriority.High, async () =>
            {
                try
                {
                    ContentDialog dialog = new()
                    {
                        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                        XamlRoot = Window.Content.XamlRoot,

                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = caption,
                        PrimaryButtonText = ResourceLoader.GetString($"Message/Button/Yes"),
                        CloseButtonText = ResourceLoader.GetString($"Message/Button/No"),
                        DefaultButton = ContentDialogButton.Close,
                        Content = message
                    };

                    await DialogSemaphore.WaitAsync();
                    try
                    {
                        Window.AppWindow.Show();
                        var result = await dialog.ShowAsync();
                        Logger.LogTrace("Closed confirm dialog {caption}: {message}, Result: {result}", caption, message, result);
                        tcs.TrySetResult(result == ContentDialogResult.Primary);
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
            var queued = DispatchService.TryEnqueue(DispatcherQueuePriority.High, async () =>
            {
                try
                {
                    ContentDialog dialog = new()
                    {
                        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                        XamlRoot = Window.Content.XamlRoot,

                        Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                        Title = caption,
                        CloseButtonText = ResourceLoader.GetString($"Message/Button/Ok"),
                        DefaultButton = ContentDialogButton.Close,
                        Content = message
                    };

                    await DialogSemaphore.WaitAsync();
                    try
                    {
                        Window.AppWindow.Show();
                        var result = await dialog.ShowAsync();
                        Logger.LogTrace("Closed success dialog {caption}: {message}", caption, message);
                        tcs.TrySetResult();
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

        public async Task ShowSuccess(AutoStartEntry autoStart, AutoStartAction action)
        {
            await ShowSuccess(autoStart.Category, autoStart.Value, autoStart.Path, action);
        }

        public async Task ShowSuccess(IgnoredAutoStart autoStart)
        {
            await ShowSuccess(autoStart.Category, autoStart.Value, autoStart.Path, AutoStartAction.RemoveIgnore);
        }

        public async Task ShowSuccess(Category autoStartCategory, string autoStartValue, string autoStartPath, AutoStartAction action)
        {
            var title = ResourceLoader.GetString($"MessageSuccess/Title/{action}");
            var type = ResourceLoader.GetString("Message/Type");
            var value = ResourceLoader.GetString("Message/Value");
            var path = ResourceLoader.GetString("Message/Path");
            await ShowSuccess(
                title,
                @$"{type}:
{autoStartCategory}

{value}:
{autoStartValue}

{path}:
{autoStartPath}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceScope.Dispose();
                    DialogSemaphore.Dispose();
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
