using AutoStartConfirm.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Windows;

namespace AutoStartConfirm.GUI
{
    public class MessageService : IMessageService, IDisposable {

        private readonly ILogger<MessageService> Logger;

        private bool disposedValue;

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        private MainWindow window = null;

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

        public MessageService(
            ILogger<MessageService> logger,
            IAppStatus appStatus)
        {
            Logger = logger;
            AppStatus = appStatus;
        }

        public void ShowError(string caption, Exception error) {
            ShowError(caption, error.Message);
        }

        public async void ShowError(string caption, string message = "") {
            ContentDialog dialog = new();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = Window.Content.XamlRoot;

            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = caption;
            dialog.PrimaryButtonText = "OK";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = message;

            var result = await dialog.ShowAsync();


            //Application.Current.Dispatcher.Invoke(delegate
            //{
            //    // Message boxes can only be shown if a parent window exists
            //    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
            //    bool newWindow = EnsureMainWindow();
            //    MessageBox.Show(MainWindow, message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            //    if (newWindow)
            //    {
            //        MainWindow.Hide();
            //    }
            //});
        }

        public bool ShowConfirm(string caption, string message = "") {
            //return Application.Current.Dispatcher.Invoke(delegate {
            //    // Message boxes can only be shown if a parent window exists
            //    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
            //    bool newWindow = EnsureMainWindow();
            //    var ret = MessageBox.Show(MainWindow, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //    if (newWindow)
            //    {
            //        MainWindow.Hide();
            //    }
            //    return ret == MessageBoxResult.Yes;
            //});
            return true;
        }

        public void ShowSuccess(string caption, string message = "") {
            //Application.Current.Dispatcher.Invoke(delegate {
            //    // Message boxes can only be shown if a parent window exists
            //    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
            //    bool newWindow = EnsureMainWindow();
            //    //MessageBox.Show(MainWindow, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            //    if (newWindow)
            //    {
            //        MainWindow.Hide();
            //    }
            //});
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
