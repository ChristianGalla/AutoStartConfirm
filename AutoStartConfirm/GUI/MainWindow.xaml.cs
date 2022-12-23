using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace AutoStartConfirm.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public bool IsClosed { get; private set; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ConnectorWindow ConnectorWindow;
        private readonly IAutoStartService AutoStartService;
        private readonly App App;

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

        public MainWindow(ConnectorWindow connectorWindow, AutoStartService autoStartService, App app)
        {
            ConnectorWindow = connectorWindow;
            AutoStartService = autoStartService;
            App = app;
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
            ConnectorWindow.Show();
        }

        #endregion
    }
}
