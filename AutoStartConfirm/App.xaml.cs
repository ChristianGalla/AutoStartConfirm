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

        public bool HasOwnAutoStart = false;

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

            HasOwnAutoStart = false;
            foreach (var autoStart in AutoStartService.CurrentAutoStarts) {
                if (IsOwnAutoStart(autoStart.Value)) {
                    HasOwnAutoStart = true;
                    break;
                }
            }
            if (HasOwnAutoStart) {
                Logger.Info("Own auto start is enabled");
            } else {
                Logger.Info("Own auto start is disabled");
            }

            if (isFirstRun) {
                AutoStartService.Add += AddHandler;
                AutoStartService.Remove += RemoveHandler;
            }
            AutoStartService.StartWatcher();
        }

        private static bool IsOwnAutoStart(AutoStartEntry autoStart) {
            return autoStart.Category == Category.CurrentUserRun64 &&
            autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
            autoStart.Value == Assembly.GetEntryAssembly().Location;
        }

        public void ToggleOwnAutoStart() {
            try {
                Logger.Info("ToggleOwnAutoStart called");
                var ownAutoStart = new RegistryAutoStartEntry() {
                    Category = Category.CurrentUserRun64,
                    Path = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm",
                    Value = Assembly.GetEntryAssembly().Location,
                    RegistryValueKind = Microsoft.Win32.RegistryValueKind.String,
                    ConfirmStatus = ConfirmStatus.New,
                };

                if (HasOwnAutoStart) {
                    Logger.Info("Shall remove own auto start");
                    AutoStartService.RemoveAutoStart(ownAutoStart);
                } else {
                    Logger.Info("Shall add own auto start");
                    AutoStartService.AddAutoStart(ownAutoStart);
                }
                Logger.Trace("Own auto start toggled");
            } catch (Exception e) {
                var message = $"Failed to change own auto start";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(e, message);
            }
        }

        private void ShowError(Exception exception, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow(true);
                MessageBox.Show(Window, exception.ToString(), message, MessageBoxButton.OK, MessageBoxImage.Error);
                if (newWindow) {
                    Window.Close();
                }
            });
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
                        autoStartService.RemoveAutoStart(autoStartEntry);
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
                        autoStartService.AddAutoStart(autoStartEntry);
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
            Logger.Trace("Toggling main window");
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

        /// <summary>
        /// Ensures that the main window is open
        /// </summary>
        /// <param name="hidden">If true creates a new hidden window if it not already exists</param>
        /// <returns>true if a new window has been created</returns>
        public static bool EnsureMainWindow(bool hidden = false) {
            Logger.Trace("Showing main window");
            bool newCreated = false;
            if (Window == null || Window.IsClosed) {
                Logger.Trace("Creating new main window");
                Window = new MainWindow();
                newCreated = true;
            }
            if (newCreated && hidden) {
                Window.WindowState = WindowState.Minimized;
                Logger.Trace("Showing main window");
                Window.Show();
            } else if (!hidden && !Window.IsVisible) {
                Logger.Trace("Showing main window");
                Window.Show();
            }
            return newCreated;
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
            EnsureMainWindow();
        }

        public void ShowRemoved(Guid id) {
            // todo: jump to removed
            Logger.Trace("ShowRemoved called");
            EnsureMainWindow();
        }

        public void RevertAdd(Guid id) {
            Logger.Info("Addition of {id} should be reverted", id);
            try {
                if (AutoStartService.TryGetAddedAutoStart(id, out AutoStartEntry autoStart)) {
                    if (AutoStartService.GetIsAdminRequiredForChanges(autoStart)) {
                        StartSubProcessAsAdmin(autoStart, RevertAddParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    } else {
                        AutoStartService.RemoveAutoStart(autoStart);
                    }
                }
            } catch (Exception e) {
                var message = "Failed to revert add";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(e, message);
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
                        AutoStartService.AddAutoStart(autoStart);
                    }
                }
            } catch (Exception e) {
                var message = "Failed to revert remove";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(e, message);
            }
        }

        public void RevertAdd(AutoStartEntry autoStart) {
            Logger.Trace("RevertAdd called");
            try {
                var autoStartService = new AutoStartService();
                autoStartService.RemoveAutoStart(autoStart);
            } catch (Exception e) {
                var message = "Failed to revert add";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(e, message);
            }
        }

        public void RevertRemove(AutoStartEntry autoStart) {
            Logger.Trace("RevertRemove called");
            try { 
                var autoStartService = new AutoStartService();
                autoStartService.AddAutoStart(autoStart);
            } catch (Exception e) {
                var message = "Failed to revert remove";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(e, message);
            }
        }

        public void ConfirmAdd(Guid id) {
            try {
                Logger.Trace("ConfirmAdd called");
                AutoStartService.ConfirmAdd(id);
            } catch (Exception e) {
                var message = $"Failed to confirm add of {id}";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(e, message);
            }
        }

        public void ConfirmRemove(Guid id) {
            try {
                Logger.Trace("ConfirmRemove called");
                AutoStartService.ConfirmRemove(id);
            } catch (Exception e) {
                var message = $"Failed to confirm remove of {id}";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(e, message);
            }
        }


        #region Event handlers
        private void AddHandler(AutoStartEntry addedAutostart) {
            Logger.Trace("AddHandler called");
            if (IsOwnAutoStart(addedAutostart)) {
                Logger.Info("Own auto start added");
                HasOwnAutoStart = true;
            }
            NotificationService.ShowNewAutoStartEntryNotification(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            Logger.Trace("RemoveHandler called");
            if (IsOwnAutoStart(removedAutostart)) {
                Logger.Info("Own auto start removed");
                HasOwnAutoStart = false;
            }
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
