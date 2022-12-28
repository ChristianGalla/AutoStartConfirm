using AutoStartConfirm.Models;
using System;
using System.Windows;

namespace AutoStartConfirm.GUI
{
    public class MessageService : IMessageService {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly MainWindow MainWindow;
        private readonly IAppStatus AppStatus;

        public MessageService(MainWindow mainWindow, IAppStatus appStatus)
        {
            MainWindow = mainWindow;
            AppStatus = appStatus;
        }

        public void ShowError(string caption, Exception error) {
            ShowError(caption, error.Message);
        }

        public void ShowError(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow();
                MessageBox.Show(MainWindow, message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                if (newWindow) {
                    MainWindow.Hide();
                }
            });
        }

        public bool ShowConfirm(string caption, string message = "") {
            return Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow();
                var ret = MessageBox.Show(MainWindow, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (newWindow) {
                    MainWindow.Hide();
                }
                return ret == MessageBoxResult.Yes;
            });
        }

        public void ShowSuccess(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow();
                MessageBox.Show(MainWindow, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                if (newWindow) {
                    MainWindow.Hide();
                }
            });
        }


        /// <summary>
        /// Ensures that the main window is open.
        /// A new hidden window is created if it not already exists.
        /// </summary>
        /// <returns>true if a new window has been created</returns>
        private bool EnsureMainWindow() {
            Logger.Trace("Showing main window");
            bool newCreated = false;
            MainWindow.Show();
            return newCreated;
        }
    }
}
