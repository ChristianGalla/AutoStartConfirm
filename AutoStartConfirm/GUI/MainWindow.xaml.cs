using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoStartConfirm.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public bool IsClosed { get; private set; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

        public AppStatus AppStatus {
            get => App.AppStatus;
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

        public ObservableCollection<AutoStartEntry> CurrentAutoStarts {
            get {
                return AutoStartService.CurrentAutoStarts;
            }
        }

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts {
            get {
                return AutoStartService.HistoryAutoStarts;
            }
        }

        public MainWindow()
        {
            Logger.Trace("Window opened");
            InitializeComponent();
        }
        protected override void OnClosed(EventArgs e)
        {
            Logger.Trace("Window closed");
            base.OnClosed(e);
            IsClosed = true;
        }

        #region Click handlers

        private void CurrentConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.ConfirmAdd(autoStartEntry);
        }

        private void CurrentRemoveButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.RevertAdd(autoStartEntry);
        }

        private void CurrentEnableButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.Enable(autoStartEntry);
        }

        private void CurrentDisableButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.Disable(autoStartEntry);
        }

        private void HistoryConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added) {
                App.ConfirmAdd(autoStartEntry);
            } else if (autoStartEntry.Change == Change.Removed) {
                App.ConfirmRemove(autoStartEntry);
            }
        }

        private void HistoryRevertButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added) {
                App.RevertAdd(autoStartEntry.Id);
            } else if (autoStartEntry.Change == Change.Removed) {
                App.RevertRemove(autoStartEntry.Id);
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e) {
            window.Close();
        }

        private void MenuItemAutoStart_Click(object sender, RoutedEventArgs e) {
            App.ToggleOwnAutoStart();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e) {
            var aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        private void MenuItemConnectors_Click(object sender, RoutedEventArgs e) {
            var connectorWindow = new ConnectorWindow();
            connectorWindow.Show();
        }

        #endregion
    }
}
