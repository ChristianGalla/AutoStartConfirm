using AutoStartConfirm.Connectors;
using System.Windows;

namespace AutoStartConfirm.Converters {
    internal class ConverterBase {
        private App app;

        private IAutoStartService autoStartService;

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

        public IAutoStartService AutoStartService {
            get {
                if (autoStartService == null) {
                    autoStartService = App.AutoStartService;
                }
                return autoStartService;
            }
            set {
                autoStartService = value;
            }
        }
    }
}