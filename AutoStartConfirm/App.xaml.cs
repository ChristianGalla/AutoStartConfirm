using AutoStartConfirm.Connectors;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.AutoStarts;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace AutoStartConfirm {
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application, IDisposable {
        private static MainWindow Window = null;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static TaskbarIcon Icon = null;

        private readonly AutoStartService AutoStartService = new AutoStartService();

        private readonly NotificationService NotificationService = new NotificationService();

        public App() {
            AutoStartService.GetCurrentAutoStarts();
            AutoStartService.Add += AddHandler;
            AutoStartService.Remove += RemoveHandler;

            try {
                AutoStartService.LoadCurrentAutoStarts();
            } catch (Exception) {
            }
        }


        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        public static void Main() {
            Logger.Info("Starting app");
            using (App app = new App()) {
                app.InitializeComponent();
                try {
                    app.Run(); // blocks until program is closing
                } catch (Exception e) {
                    Logger.Error(new Exception("Failed to run", e));
                }
                Logger.Info("Finished");
            }
        }

        void App_Startup(object sender, StartupEventArgs e) {
            Icon = (TaskbarIcon)FindResource("NotifyIcon");
            AutoStartService.StartWatcher();
        }

        public static void ToggleMainWindow() {
            Logger.Info("Toggling main window");
            if (Window == null || Window.IsClosed) {
                Logger.Trace("Creating new main window");
                Window = new MainWindow();
            }
            if (Window.IsVisible) {
                Logger.Trace("Closing main window");
                Window.Close();
            } else {
                Logger.Trace("Showing main window");
                Window.Show();
            }
        }

        public static void ShowMainWindow() {
            Logger.Info("Toggling main window");
            if (Window == null || Window.IsClosed) {
                Logger.Trace("Creating new main window");
                Window = new MainWindow();
            }
            if (!Window.IsVisible) {
                Logger.Trace("Showing main window");
                Window.Show();
            }
        }

        internal static void Close() {
            Logger.Info("Closing application");
            try {
                Current.Shutdown();
            } catch (Exception e) {
                Logger.Error(e);
            }
        }

        protected override void OnExit(ExitEventArgs e) {
            try {
                AutoStartService.SaveAutoStarts();
            } finally {
                base.OnExit(e);
            }
        }


        #region Event handlers
        private void AddHandler(AutoStartEntry addedAutostart) {
            NotificationService.ShowNewAutoStartEntryNotification(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            NotificationService.ShowRemovedAutoStartEntryNotification(removedAutostart);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    AutoStartService.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
