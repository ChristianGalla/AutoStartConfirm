using System.Windows;

namespace AutoStartConfirm.GUI
{
    public partial class NotifyIcon {
        private App app;

        public App App {
            get {
                if (app == null) {
                    app = (App)Application.Current;
                }
                return app;
            }
            set {
                app = value;
            }
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            App.Close();
        }

        private void OwnAutoStartClicked(object sender, RoutedEventArgs e) {
            Application.Current.Dispatcher.Invoke(delegate {
                App.ToggleOwnAutoStart();
            });
        }
    }
}
