using System.Windows;

namespace AutoStartConfirm.Models
{
    public class NotifyIconViewModel {
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

        public AppStatus AppStatus {
            get => App.AppStatus;
        }
    }
}
