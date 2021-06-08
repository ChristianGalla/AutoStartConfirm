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
    public partial class MainWindow : Window, IDisposable {
        public bool IsClosed { get; private set; }

        private bool CurrentAutoStartGridNeedsRefresh { get; set; } = false;

        private bool HistoryAutoStartGridNeedsRefresh { get; set; } = false;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private App App {
            get {
                return App.GetInstance();
            }
        }

        public Dictionary<Guid, AutoStartEntry>.ValueCollection CurrentAutoStarts {
            get {
                return App.AutoStartService.CurrentAutoStarts.Values;
            }
        }

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts {
            get {
                return App.AutoStartService.HistoryAutoStarts;
            }
        }

        public bool HasOwnAutoStart {
            get {
                return App.HasOwnAutoStart;
            }
        }

        private Timer RefreshTimer;

        public MainWindow()
        {
            Logger.Trace("Window opened");
            InitializeComponent();
            App.AutoStartService.CurrentAutoStartChange += CurrentAutoStartChangeHandler;
            App.AutoStartService.HistoryAutoStartChange += HistoryAutoStartChangeHandler;

            RefreshTimer = new Timer(2000);
            RefreshTimer.Elapsed += OnTimedEvent;
            RefreshTimer.AutoReset = true;
            RefreshTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            try {
                Application.Current?.Dispatcher.Invoke(delegate {
                    if (CurrentAutoStartGridNeedsRefresh) {
                        CurrentAutoStartGrid.Items.Refresh();
                        CurrentAutoStartGridNeedsRefresh = false;
                    }
                    if (HistoryAutoStartGridNeedsRefresh) {
                        HistoryAutoStartGrid.Items.Refresh();
                        HistoryAutoStartGridNeedsRefresh = false;
                    }
                });
            } catch (Exception) {
            }
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
            App.ConfirmAdd(autoStartEntry.Id);
        }

        private void CurrentRemoveButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.RevertAdd(autoStartEntry.Id);
        }

        private void CurrentEnableButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.Enable(autoStartEntry.Id);
        }

        private void CurrentDisableButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.Disable(autoStartEntry.Id);
        }

        private void HistoryConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added) {
                App.ConfirmAdd(autoStartEntry.Id);
            } else if (autoStartEntry.Change == Change.Removed) {
                App.ConfirmRemove(autoStartEntry.Id);
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
            App.GetInstance().ToggleOwnAutoStart();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e) {
            var aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }

        #endregion


        #region Event handlers

        private void CurrentAutoStartChangeHandler(AutoStartEntry autostart) {
            CurrentAutoStartGridNeedsRefresh = true;
        }

        private void HistoryAutoStartChangeHandler(AutoStartEntry autostart) {
            HistoryAutoStartGridNeedsRefresh = true;
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    RefreshTimer.Stop();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
