using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoStartConfirm.GUI {
    public class MessageService : IMessageService {
        private MainWindow window;

        public MainWindow Window {
            get {
                if (window == null) {
                    window = (MainWindow)Application.Current.MainWindow;
                }
                return window;
            }
            set {
                window = value;
            }
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public void ShowError(string caption, Exception error) {
            ShowError(caption, error.Message);
        }

        public void ShowError(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow();
                MessageBox.Show(Window, message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                if (newWindow) {
                    Window.Close();
                }
            });
        }

        public bool ShowConfirm(string caption, string message = "") {
            return Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow();
                var ret = MessageBox.Show(Window, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (newWindow) {
                    Window.Close();
                }
                return ret == MessageBoxResult.Yes;
            });
        }

        public void ShowSuccess(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow();
                MessageBox.Show(Window, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
                if (newWindow) {
                    Window.Close();
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
            if (Window == null || Window.IsClosed) {
                Logger.Trace("Creating new main window");
                Window = new MainWindow();
                newCreated = true;
            }
            if (newCreated) {
                Window.WindowState = WindowState.Minimized;
                Logger.Trace("Showing main window");
                Window.Show();
            }
            return newCreated;
        }
    }
}
