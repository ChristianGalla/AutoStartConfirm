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
using Windows.Foundation.Collections;

namespace AutoStartConfirm {
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application, IDisposable {
        private static MainWindow Window = null;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static TaskbarIcon Icon = null;

        public bool HasOwnAutoStart = false;

        private IAutoStartService autoStartService;

        public IAutoStartService AutoStartService {
            get {
                if (autoStartService == null) {
                    autoStartService = new AutoStartService();
                }
                return autoStartService;
            }
            set {
                autoStartService = value;
            }
        }

        private INotificationService notificationService;

        public INotificationService NotificationService {
            get {
                if (notificationService == null) {
                    notificationService = new NotificationService();
                }
                return notificationService;
            }
            set {
                notificationService = value;
            }
        }

        private static App AppInstance;

        private static readonly string RevertAddParameterName = "--revertAdd";

        private static readonly string RevertRemoveParameterName = "--revertRemove";

        private static readonly string EnableParameterName = "--enable";

        private static readonly string DisableParameterName = "--disable";

        public App() {
            AppInstance = this;
        }

        public void Start(bool skipInitializing = false) {
            // disable notifications for new added auto starts on first start to avoid too many notifications at once
            bool isFirstRun = AutoStartService.GetAutoStartFileExists();
            if (!isFirstRun) {
                AutoStartService.Add += AddHandler;
                AutoStartService.Remove += RemoveHandler;
                AutoStartService.Enable += EnableHandler;
                AutoStartService.Disable += DisableHandler;
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
                AutoStartService.Enable += EnableHandler;
                AutoStartService.Disable += DisableHandler;
            }
            AutoStartService.StartWatcher();

            if (!skipInitializing) {
                InitializeComponent();
            }
        }

        private static bool IsOwnAutoStart(AutoStartEntry autoStart) {
            return autoStart.Category == Category.CurrentUserRun64 &&
            autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
            autoStart.Value == Assembly.GetEntryAssembly().Location;
        }

        public void ToggleOwnAutoStart() {
            Task.Run(() => {
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
                    HasOwnAutoStart = !HasOwnAutoStart;
                    ownAutoStart.ConfirmStatus = ConfirmStatus.New;
                    Logger.Trace("Own auto start toggled");
                } catch (Exception e) {
                    var message = "Failed to change own auto start";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    ShowError(message, e);
                }
            });
        }

        public void ShowError(string caption, Exception error) {
            ShowError(caption, error.Message);
        }

        public void ShowError(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow(true);
                MessageBox.Show(Window, message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
                if (newWindow) {
                    Window.Close();
                }
            });
        }

        public bool ShowConfirm(string caption, string message = "") {
            return Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow(true);
                var ret = MessageBox.Show(Window, message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (newWindow) {
                    Window.Close();
                }
                return ret == MessageBoxResult.Yes;
            });
        }

        public void ShowSuccess(string caption, string message = "") {
            Application.Current.Dispatcher.Invoke(delegate {
                // Message boxes can only be shown if a parent window exists
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/116bcd83-93bf-42f3-9bfe-da9e7de37546/messagebox-closes-immediately-in-dispatcherunhandledexception-handler?forum=wpf
                bool newWindow = EnsureMainWindow(true);
                MessageBox.Show(Window, message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
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
                using (App app = new App()) {
                    Logger.Info("Parameters: {args}", args);
                    if (app.HandleCommandLineParameters(args)) {
                        return 0;
                    }
                    Logger.Info("Normal start");
                    app.Start();
                    app.Run(); // blocks until program is closing
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

        /// <summary>
        /// Handles auto starts if command line parameters are set
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns>True, if parameters were set, correctly handled and the program can be closed</returns>
        private bool HandleCommandLineParameters(string[] args) {
            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];
                if (string.Equals(arg, RevertAddParameterName, StringComparison.OrdinalIgnoreCase)) {
                    Logger.Info("Adding should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.RemoveAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                } else if (string.Equals(arg, RevertRemoveParameterName, StringComparison.OrdinalIgnoreCase)) {
                    Logger.Info("Removing should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.AddAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                } else if (string.Equals(arg, EnableParameterName, StringComparison.OrdinalIgnoreCase)) {
                    Logger.Info("Auto start should be enabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.EnableAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                } else if (string.Equals(arg, DisableParameterName, StringComparison.OrdinalIgnoreCase)) {
                    Logger.Info("Auto start should be disabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.DisableAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
            }
            return false;
        }

        private static AutoStartEntry LoadAutoStartFromParameter(string[] args, int i) {
            if (i + 1 >= args.Length) {
                throw new ArgumentException("Missing path to file");
            }
            var path = args[i + 1];
            var autoStartEntry = LoadAutoStartFromFile(path);
            return autoStartEntry;
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
            if (AutoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart)) {
                RevertAdd(autoStart);
            } else {
                var message = "Failed to get auto start to remove";
                Logger.Error(message);
                ShowError(message);
            }
        }

        public void RevertAdd(AutoStartEntry autoStart) {
            Task.Run(() => {
                Logger.Info("Should add {@autoStart}", autoStart);
                try {
                    if (!ShowConfirm("Confirm remove", $"Are you sure you want to remove \"{autoStart.Value}\" from auto starts?")) {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart)) {
                        StartSubProcessAsAdmin(autoStart, RevertAddParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    } else {
                        AutoStartService.RemoveAutoStart(autoStart);
                    }
                    ShowSuccess("Auto start removed", $"\"{autoStart.Value}\" has been removed from auto starts.");
                } catch (Exception e) {
                    var message = "Failed to revert add";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    ShowError(message, e);
                }
            });
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
            if (AutoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart)) {
                RevertRemove(autoStart);
            } else {
                var message = "Failed to get auto start to add";
                Logger.Error(message);
                ShowError(message);
            }
        }

        public void RevertRemove(AutoStartEntry autoStart) {
            Task.Run(() => {
                Logger.Info("Should remove {@autoStart}", autoStart);
                try {
                    if (!ShowConfirm("Confirm add", $"Are you sure you want to add \"{autoStart.Value}\" as auto start?")) {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart)) {
                        StartSubProcessAsAdmin(autoStart, RevertRemoveParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    } else {
                        AutoStartService.AddAutoStart(autoStart);
                    }
                    ShowSuccess("Auto start added", $"\"{autoStart.Value}\" has been added to auto starts.");
                } catch (Exception e) {
                    var message = "Failed to revert remove";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    ShowError(message, e);
                }
            });
        }

        public void Enable(Guid id) {
            Logger.Info("{id} should be enabled", id);
            if (AutoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart)) {
                Enable(autoStart);
            } else {
                var message = "Failed to get auto start to enable";
                Logger.Error(message);
                ShowError(message);
            }
        }

        public void Enable(AutoStartEntry autoStart) {
            Task.Run(() => {
                Logger.Info("Should enable {@autoStart}", autoStart);
                try {
                    if (!ShowConfirm("Confirm enable", $"Are you sure you want to enable auto start \"{autoStart.Value}\"?")) {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart)) {
                        StartSubProcessAsAdmin(autoStart, EnableParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Enabled;
                    } else {
                        AutoStartService.EnableAutoStart(autoStart);
                    }
                    ShowSuccess("Auto start enabled", $"\"{autoStart.Value}\" has been enabled.");
                } catch (Exception e) {
                    var message = "Failed to enable";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    ShowError(message, e);
                }
            });
        }

        public void Disable(Guid id) {
            Logger.Info("{id} should be disabled", id);
            if (AutoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart)) {
                Disable(autoStart);
            } else {
                var message = "Failed to get auto start to disable";
                Logger.Error(message);
                ShowError(message);
            }
        }

        public void Disable(AutoStartEntry autoStart) {
            Task.Run(() => {
                Logger.Info("Should disable {@autoStart}", autoStart);
                try {
                    if (!ShowConfirm("Confirm disable", $"Are you sure you want to disable auto start \"{autoStart.Value}\"?")) {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart)) {
                        StartSubProcessAsAdmin(autoStart, DisableParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Disabled;
                    } else {
                        AutoStartService.DisableAutoStart(autoStart);
                    }
                    ShowSuccess("Auto start disabled", $"\"{autoStart.Value}\" has been disabled.");
                } catch (Exception e) {
                    var message = "Failed to disable";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    ShowError(message, e);
                }
            });
        }

        public void ConfirmAdd(Guid id) {
            try {
                Logger.Trace("ConfirmAdd called");
                AutoStartService.ConfirmAdd(id);
            } catch (Exception e) {
                var message = $"Failed to confirm add of {id}";
                var err = new Exception(message, e);
                Logger.Error(err);
                ShowError(message, e);
            }
        }

        public void ConfirmRemove(Guid id) {
            Task.Run(() => {
                try {
                    Logger.Trace("ConfirmRemove called");
                    AutoStartService.ConfirmRemove(id);
                } catch (Exception e) {
                    var message = $"Failed to confirm remove of {id}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    ShowError(message, e);
                }
            });
        }

        #region Event handlers
        protected override void OnStartup(StartupEventArgs e) {
            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs => {
                Logger.Trace("Toast activated {Arguments}", toastArgs.Argument);
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;

                // Need to dispatch to UI thread if performing UI operations
                Application.Current.Dispatcher.Invoke(delegate {
                    Logger.Trace("Handling action {Arguments} {UserInput}", toastArgs.Argument, userInput);
                    switch (args["action"]) {
                        case "viewRemove":
                            ShowRemoved(Guid.Parse(args["id"]));
                            break;
                        case "revertRemove":
                            RevertRemove(Guid.Parse(args["id"]));
                            break;
                        case "confirmRemove":
                            ConfirmRemove(Guid.Parse(args["id"]));
                            break;
                        case "viewAdd":
                            ShowAdd(Guid.Parse(args["id"]));
                            break;
                        case "revertAdd":
                            RevertAdd(Guid.Parse(args["id"]));
                            break;
                        case "confirmAdd":
                            ConfirmAdd(Guid.Parse(args["id"]));
                            break;
                        case "confirmEnable":
                            ConfirmAdd(Guid.Parse(args["id"]));
                            break;
                        case "confirmDisable":
                            ConfirmAdd(Guid.Parse(args["id"]));
                            break;
                        case "enable":
                            Enable(Guid.Parse(args["id"]));
                            break;
                        case "disable":
                            Disable(Guid.Parse(args["id"]));
                            break;
                        default:
                            Logger.Trace("Unknown action {Action}", args["action"]);
                            break;
                    }
                });
            };
            base.OnStartup(e);
        }

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

        private void EnableHandler(AutoStartEntry enabledAutostart) {
            Logger.Trace("EnableHandler called");
            NotificationService.ShowEnabledAutoStartEntryNotification(enabledAutostart);
        }

        private void DisableHandler(AutoStartEntry disabledAutostart) {
            Logger.Trace("DisableHandler called");
            NotificationService.ShowDisabledAutoStartEntryNotification(disabledAutostart);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (AutoStartService != null) {
                        AutoStartService.Dispose();
                    }
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
