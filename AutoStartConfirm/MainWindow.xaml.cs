using AutoStartConfirm.AutoStarts;
using System;
using System.Collections.Generic;
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

namespace AutoStartConfirm {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable {
        public bool IsClosed { get; private set; }

        private bool CurrentAutoStartGridNeedsRefresh { get; set; } = false;

        private bool AddAutoStartGridNeedsRefresh { get; set; } = false;

        private bool RemovedAutoStartGridNeedsRefresh { get; set; } = false;

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

        public Dictionary<Guid, AutoStartEntry>.ValueCollection AddedAutoStarts {
            get {
                return App.AutoStartService.AddedAutoStarts.Values;
            }
        }

        public Dictionary<Guid, AutoStartEntry>.ValueCollection RemovedAutoStarts {
            get {
                return App.AutoStartService.RemovedAutoStarts.Values;
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
            App.AutoStartService.AddAutoStartChange += AddAutoStartChangeHandler;
            App.AutoStartService.RemoveAutoStartChange += RemoveAutoStartChangeHandler;

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
                    if (AddAutoStartGridNeedsRefresh) {
                        AddedAutoStartGrid.Items.Refresh();
                        AddAutoStartGridNeedsRefresh = false;
                    }
                    if (RemovedAutoStartGridNeedsRefresh) {
                        RemovedAutoStartGrid.Items.Refresh();
                        RemovedAutoStartGridNeedsRefresh = false;
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
            AddConfirmButton_Click(sender, e);
        }

        private void CurrentRemoveButton_Click(object sender, RoutedEventArgs e) {
            AddRevertButton_Click(sender, e);
        }

        private void CurrentEnableButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.Enable(autoStartEntry.Id);
        }

        private void CurrentDisableButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.Disable(autoStartEntry.Id);
        }

        private void AddConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.ConfirmAdd(autoStartEntry.Id);
        }

        private void AddRevertButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.RevertAdd(autoStartEntry.Id);
        }

        private void RemoveConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.ConfirmRemove(autoStartEntry.Id);
        }

        private void RemoveRevertButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.RevertRemove(autoStartEntry.Id);
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

        private void CurrentAutoStartChangeHandler(AutoStartEntry addedAutostart) {
            CurrentAutoStartGridNeedsRefresh = true;
        }

        private void AddAutoStartChangeHandler(AutoStartEntry addedAutostart) {
            AddAutoStartGridNeedsRefresh = true;
        }

        private void RemoveAutoStartChangeHandler(AutoStartEntry addedAutostart) {
            RemovedAutoStartGridNeedsRefresh = true;
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
