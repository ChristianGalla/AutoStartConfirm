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
using System.Diagnostics;
using System.Reflection;

namespace AutoStartConfirm {
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application, IDisposable {
        private static MainWindow Window = null;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static TaskbarIcon Icon = null;

        public readonly AutoStartService AutoStartService = new AutoStartService();

        private readonly NotificationService NotificationService = new NotificationService();

        private static App AppInstance;

        private static readonly string RevertAddParameterName = "--revertAdd";

        private static readonly string RevertRemoveParameterName = "--revertRemove";

        private App() {
            // disable notifications for new added auto starts on first start to avoid too many notifications at once
            bool isFirstRun = AutoStartService.GetAutoStartFileExists();
            if (!isFirstRun) {
                AutoStartService.Add += AddHandler;
                AutoStartService.Remove += RemoveHandler;
            }

            try {
                AutoStartService.LoadCurrentAutoStarts();
            } catch (Exception) {
            }
            if (isFirstRun) {
                AutoStartService.Add += AddHandler;
                AutoStartService.Remove += RemoveHandler;
            }
            AutoStartService.StartWatcher();
        }

        public static App GetInstance() {
            return AppInstance;
        }

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        public static int Main(string[] args) {
            try {
                Logger.Info("Starting");
                Logger.Info("Parameters: {args}", args);
                for (int i = 0; i < args.Length; i++) {
                    var arg = args[i];
                    if (string.Equals(arg, RevertAddParameterName, StringComparison.OrdinalIgnoreCase)) {
                        Logger.Info("Adding should be reverted");
                        if (i + 1 >= args.Length) {
                            throw new ArgumentException("Missing path to file");
                        }
                        var path = args[i + 1];
                        var autoStartEntry = LoadAutoStartFromFile(path);
                        var autoStartService = new AutoStartService();
                        autoStartService.RevertAdd(autoStartEntry);
                        Logger.Info("Finished");
                        return 0;
                    } else if (string.Equals(arg, RevertRemoveParameterName, StringComparison.OrdinalIgnoreCase)) {
                        Logger.Info("Removing should be reverted");
                        if (i + 1 >= args.Length) {
                            throw new ArgumentException("Missing path to file");
                        }
                        var path = args[i + 1];
                        var autoStartEntry = LoadAutoStartFromFile(path);
                        var autoStartService = new AutoStartService();
                        autoStartService.RevertRemove(autoStartEntry);
                        Logger.Info("Finished");
                        return 0;
                    }
                }
                Logger.Info("Normal start");
                using (App app = new App()) {
                    AppInstance = app;
                    app.InitializeComponent();
                    try {
                        app.Run(); // blocks until program is closing
                    } catch (Exception e) {
                        Logger.Error(new Exception("Failed to run", e));
                    }
                    Logger.Info("Finished");
                }
                AppInstance = null;
                return 0;
            } catch (Exception e) {
                var err = new Exception("Failed to run", e);
                Logger.Error(err);
                return 1;
            }
        }

        private static AutoStartEntry LoadAutoStartFromFile(string path) {
            Logger.Trace("LoadAutoStartFromFile called");
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                IFormatter formatter = new BinaryFormatter();
                try {
                    var ret = (AutoStartEntry)formatter.Deserialize(stream);
                    return ret;
                } catch (Exception ex) {
                    var err = new Exception("Failed to deserialize", ex);
                    throw err;
                }
            }
        }

        void App_Startup(object sender, StartupEventArgs e) {
            Logger.Trace("App_Startup called");
            Icon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        public static void ToggleMainWindow() {
            Logger.Debug("Toggling main window");
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
            Logger.Debug("Toggling main window");
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

        public void ShowAdd(Guid id) {
            // todo: jump to added
            Logger.Trace("ShowAdd called");
            ShowMainWindow();
        }

        public void ShowRemoved(Guid id) {
            // todo: jump to removed
            Logger.Trace("ShowRemoved called");
            ShowMainWindow();
        }

        public void RevertAdd(Guid id) {
            Logger.Info("Addition of {id} should be reverted", id);
            try {
                if (AutoStartService.TryGetAddedAutoStart(id, out AutoStartEntry autoStart)) {
                    if (AutoStartService.GetIsAdminRequiredForChanges(autoStart)) {
                        StartSubProcessAsAdmin(autoStart, RevertAddParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    } else {
                        AutoStartService.RevertAdd(autoStart);
                    }
                }
            } catch (Exception e) {
                var err = new Exception("Failed to revert add", e);
                Logger.Error(err);
            }
        }

        private static void StartSubProcessAsAdmin(AutoStartEntry autoStart, string parameterName) {
            Logger.Trace("StartSubProcessAsAdmin called");
            string path = Path.GetTempFileName();
            try {
                using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, autoStart);
                }

                var info = new ProcessStartInfo(
                    Assembly.GetEntryAssembly().Location,
                    $"{parameterName} {path}") {
                    Verb = "runas", // indicates to elevate privileges
                };

                var process = new Process {
                    EnableRaisingEvents = true, // enable WaitForExit()
                    StartInfo = info
                };

                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0) {
                    throw new Exception("Sub process failed to execute");
                }
            } finally {
                File.Delete(path);
            }
        }

        public void RevertRemove(Guid id) {
            Logger.Info("Removal of {id} should be reverted", id);
            try {
                if (AutoStartService.TryGetRemovedAutoStart(id, out AutoStartEntry autoStart)) {
                    if (AutoStartService.GetIsAdminRequiredForChanges(autoStart)) {
                        StartSubProcessAsAdmin(autoStart, RevertRemoveParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    } else {
                        AutoStartService.RevertRemove(autoStart);
                    }
                }
            } catch (Exception e) {
                var err = new Exception("Failed to revert remove", e);
                Logger.Error(err);
            }
        }

        public static void RevertAdd(AutoStartEntry autoStart) {
            Logger.Trace("RevertAdd called");
            try {
                var autoStartService = new AutoStartService();
                autoStartService.RevertAdd(autoStart);
            } catch (Exception e) {
                var err = new Exception("Failed to revert add", e);
                Logger.Error(err);
            }
        }

        public static void RevertRemove(AutoStartEntry autoStart) {
            Logger.Trace("RevertRemove called");
            try { 
                var autoStartService = new AutoStartService();
                autoStartService.RevertRemove(autoStart);
            } catch (Exception e) {
                var err = new Exception("Failed to revert remove", e);
                Logger.Error(err);
            }
        }

        public void ConfirmAdd(Guid id) {
            try {
                Logger.Trace("ConfirmAdd called");
                AutoStartService.ConfirmAdd(id);
            } catch (Exception e) {
                var err = new Exception($"Failed to confirm add of {id}", e);
                Logger.Error(err);
            }
        }

        public void ConfirmRemove(Guid id) {
            Logger.Trace("ConfirmRemove called");
            AutoStartService.ConfirmRemove(id);
        }


        #region Event handlers
        private void AddHandler(AutoStartEntry addedAutostart) {
            Logger.Trace("AddHandler called");
            NotificationService.ShowNewAutoStartEntryNotification(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            Logger.Trace("RemoveHandler called");
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
