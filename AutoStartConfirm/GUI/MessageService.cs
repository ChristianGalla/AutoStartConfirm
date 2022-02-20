using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoStartConfirm.GUI {
    public class MessageService : IMessageService {
        private MainWindow _window;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MessageService(MainWindow window)
        {
            _window = window;
        }

        public void ShowError(string caption, Exception error) {
            ShowError(caption, error.Message);
        }

        public void ShowError(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                MessageBox.Show(_window, message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        public bool ShowConfirm(string caption, string message = "") {
            return Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                var ret = MessageBox.Show(_window, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return ret == MessageBoxResult.Yes;
            });
        }

        public void ShowSuccess(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                MessageBox.Show(_window, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }


        /// <summary>
        /// Ensures that the main window is open.
        /// </summary>
        private void EnsureMainWindow() {
            Logger.Trace("Showing main window");
            if (_window == null || _window.IsClosed) {
                throw new Exception("Main window not existsing");
            }
            _window.WindowState = WindowState.Minimized;
            Logger.Trace("Showing main window");
            _window.Show();
        }
    }
}
