using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutoStartConfirm.GUI {
    /// <summary>
    /// Interaction logic for ConnectorWindow.xaml
    /// </summary>
    public partial class ConnectorWindow : Window, IDisposable {

        protected ObservableCollection<ConnectorEnableRow> connectors;
        private bool disposedValue;

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


        private ISettingsService settingsService;

        public ISettingsService SettingsService {
            get {
                if (settingsService == null) {
                    settingsService = App.SettingsService;
                }
                return settingsService;
            }
            set => settingsService = value;
        }

        public ObservableCollection<ConnectorEnableRow> Connectors {
            get {
                if (connectors == null) {
                    connectors = new ObservableCollection<ConnectorEnableRow>();
                    connectors.CollectionChanged += CollectionChanged;
                    foreach (Category category in Enum.GetValues(typeof(Category))) {
                        var row = new ConnectorEnableRow() {
                            Category = category,
                            Enabled = !SettingsService.DisabledConnectors.Contains(category.ToString())
                        };
                        connectors.Add(row);
                    }
                }
                return connectors;
            }
        }

        public ConnectorWindow() {
            InitializeComponent();
        }
        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Add) {
                foreach (ConnectorEnableRow newItem in e.NewItems) {
                    newItem.PropertyChanged += PropertyChangedHandler;
                }
            }
        }

        private void PropertyChangedHandler(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName != "Enabled") {
                return;
            }
            var changedItem = (ConnectorEnableRow)sender;
            var categoryString = changedItem.Category.ToString();
            if (changedItem.Enabled) {
                while (SettingsService.DisabledConnectors.Contains(categoryString)) {
                    SettingsService.DisabledConnectors.Remove(categoryString);
                }
            } else {
                SettingsService.DisabledConnectors.Add(categoryString);
            }
            SettingsService.Save();
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (connectors != null) {
                        connectors.CollectionChanged -= CollectionChanged;
                        foreach (var connetor in connectors) {
                            connetor.PropertyChanged -= PropertyChangedHandler;
                        }
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
