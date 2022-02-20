using AutoStartConfirm.Connectors;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Models;
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
using AutoStartConfirm.GUI;
using System.Collections.ObjectModel;
using AutoStartConfirm.Properties;
using System.Collections.Specialized;
using AutoStartConfirm.Connectors.Registry;

namespace AutoStartConfirm
{
    public class Business : IBusiness
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly MainWindow _mainWindow;

        private readonly IAppStatus _appStatus;

        private readonly IAutoStartService _autoStartService;

        private readonly INotificationService _notificationService;

        private readonly IMessageService _messageService;

        private readonly ISettingsService _settingsService;

        private static readonly string RevertAddParameterName = "--revertAdd";

        private static readonly string RevertRemoveParameterName = "--revertRemove";

        private static readonly string EnableParameterName = "--enable";

        private static readonly string DisableParameterName = "--disable";

        public Business(
            IAutoStartService autoStartService,
            INotificationService notificationService,
            IMessageService messageService,
            ISettingsService settingsService,
            IAppStatus appStatus,
            MainWindow mainWindow)
        {
            _autoStartService = autoStartService;
            _notificationService = notificationService;
            _messageService = messageService;
            _settingsService = settingsService;
            _appStatus = appStatus;
            _mainWindow = mainWindow;
        }

        public void Start(bool skipInitializing = false)
        {
            // disable notifications for new added auto starts on first start to avoid too many notifications at once
            //bool isFirstRun = !_autoStartService.GetValidAutoStartFileExists();
            //if (!isFirstRun)
            //{
            //    _autoStartService.Add += AddHandler;
            //    _autoStartService.Remove += RemoveHandler;
            //    _autoStartService.Enable += EnableHandler;
            //    _autoStartService.Disable += DisableHandler;
            //}

            //try
            //{
            //    _autoStartService.LoadCurrentAutoStarts();
            //    _appStatus.HasOwnAutoStart = _autoStartService.HasOwnAutoStart;
            //}
            //catch (Exception)
            //{
            //}

            //if (isFirstRun)
            //{
            //    _autoStartService.Add += AddHandler;
            //    _autoStartService.Remove += RemoveHandler;
            //    _autoStartService.Enable += EnableHandler;
            //    _autoStartService.Disable += DisableHandler;
            //}
            //_autoStartService.StartWatcher();

            //if (!skipInitializing)
            //{
            //    InitializeComponent();
            //}
        }

        public Task ToggleOwnAutoStart()
        {
            return Task.Run(() =>
            {
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    _autoStartService.ToggleOwnAutoStart();
                }
                catch (Exception e)
                {
                    var message = "Failed to change own auto start";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        //[System.STAThreadAttribute()]
        //public static int Main(string[] args)
        //{
        //    try
        //    {
        //        Logger.Info("Starting");
        //        using (App app = new App())
        //        {
        //            Logger.Info("Parameters: {args}", args);
        //            if (app.HandleCommandLineParameters(args))
        //            {
        //                return 0;
        //            }
        //            Logger.Info("Normal start");
        //            app.Start();
        //            app.Run(); // blocks until program is closing
        //            Logger.Info("Finished");
        //        }
        //        AppInstance = null;
        //        return 0;
        //    }
        //    catch (Exception e)
        //    {
        //        var err = new Exception("Failed to run", e);
        //        Logger.Error(err);
        //        return 1;
        //    }
        //}

        /// <summary>
        /// Handles auto starts if command line parameters are set
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns>True, if parameters were set, correctly handled and the program can be closed</returns>
        private bool HandleCommandLineParameters(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, RevertAddParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Adding should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    _autoStartService.RemoveAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
                else if (string.Equals(arg, RevertRemoveParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Removing should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    _autoStartService.AddAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
                else if (string.Equals(arg, EnableParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Auto start should be enabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    _autoStartService.EnableAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
                else if (string.Equals(arg, DisableParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Auto start should be disabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    _autoStartService.DisableAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
            }
            return false;
        }

        private static AutoStartEntry LoadAutoStartFromParameter(string[] args, int i)
        {
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException("Missing path to file");
            }
            var path = args[i + 1];
            var autoStartEntry = LoadAutoStartFromFile(path);
            return autoStartEntry;
        }

        private static AutoStartEntry LoadAutoStartFromFile(string path)
        {
            Logger.Trace("LoadAutoStartFromFile called");
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                IFormatter formatter = new BinaryFormatter();
                try
                {
                    var ret = (AutoStartEntry)formatter.Deserialize(stream);
                    return ret;
                }
                catch (Exception ex)
                {
                    var err = new Exception("Failed to deserialize", ex);
                    throw err;
                }
            }
        }

        public void ToggleMainWindow()
        {
            Logger.Trace("Toggling main window");
            if (_mainWindow == null || _mainWindow.IsClosed)
            {
                throw new Exception("Main Window not exists");
            }
            if (_mainWindow.IsVisible)
            {
                Logger.Trace("Closing main window");
                _mainWindow.Close();
            }
            else
            {
                Logger.Trace("Showing main window");
                _mainWindow.Show();
            }
        }

        /// <summary>
        /// Ensures that the main window is open
        /// </summary>
        /// <param name="hidden">If true creates a new hidden window if it not already exists</param>
        public void EnsureMainWindow(bool hidden = false)
        {
            Logger.Trace("Showing main window");
            if (_mainWindow == null || _mainWindow.IsClosed)
            {
                throw new Exception("Main Window not exists");
            }
            if (hidden)
            {
                _mainWindow.WindowState = WindowState.Minimized;
                Logger.Trace("Showing main window");
                _mainWindow.Show();
            }
            else if (!hidden && !_mainWindow.IsVisible)
            {
                Logger.Trace("Showing main window");
                _mainWindow.Show();
            }
        }

        //protected override void OnExit(ExitEventArgs e)
        //{
        //    try
        //    {
        //        _autoStartService.SaveAutoStarts();
        //    }
        //    finally
        //    {
        //        try
        //        {
        //            Icon.Dispose();
        //        }
        //        catch (Exception)
        //        {
        //        }
        //        base.OnExit(e);
        //    }
        //}

        public void ShowAdd(Guid id)
        {
            // todo: jump to added
            Logger.Trace("ShowAdd called");
            EnsureMainWindow();
        }

        public void ShowRemoved(Guid id)
        {
            // todo: jump to removed
            Logger.Trace("ShowRemoved called");
            EnsureMainWindow();
        }

        public void RevertAdd(Guid id)
        {
            Logger.Info("Addition of {id} should be reverted", id);
            if (_autoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart))
            {
                RevertAdd(autoStart);
            }
            else
            {
                var message = "Failed to get auto start to remove";
                Logger.Error(message);
                _messageService.ShowError(message);
            }
        }

        public void RevertAdd(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.Info("Should add {@autoStart}", autoStart);
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    if (!_messageService.ShowConfirm("Confirm remove", $"Are you sure you want to remove \"{autoStart.Value}\" from auto starts?"))
                    {
                        return;
                    }
                    if (_autoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, RevertAddParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    }
                    else
                    {
                        _autoStartService.RemoveAutoStart(autoStart);
                    }
                    _messageService.ShowSuccess("Auto start removed", $"\"{autoStart.Value}\" has been removed from auto starts.");
                }
                catch (Exception e)
                {
                    var message = "Failed to revert add";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        private void StartSubProcessAsAdmin(AutoStartEntry autoStart, string parameterName)
        {
            Logger.Trace("StartSubProcessAsAdmin called");
            string path = Path.GetTempFileName();
            try
            {
                using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, autoStart);
                }

                var info = new ProcessStartInfo(
                    _autoStartService.CurrentExePath,
                    $"{parameterName} {path}")
                {
                    Verb = "runas", // indicates to elevate privileges
                };

                var process = new Process
                {
                    EnableRaisingEvents = true, // enable WaitForExit()
                    StartInfo = info
                };

                process.Start();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception("Sub process failed to execute");
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        public void RevertRemove(Guid id)
        {
            Logger.Info("Removal of {id} should be reverted", id);
            if (_autoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart))
            {
                RevertRemove(autoStart);
            }
            else
            {
                var message = "Failed to get auto start to add";
                Logger.Error(message);
                _messageService.ShowError(message);
            }
        }

        public void RevertRemove(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.Info("Should remove {@autoStart}", autoStart);
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    if (!_messageService.ShowConfirm("Confirm add", $"Are you sure you want to add \"{autoStart.Value}\" as auto start?"))
                    {
                        return;
                    }
                    if (_autoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, RevertRemoveParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    }
                    else
                    {
                        _autoStartService.AddAutoStart(autoStart);
                    }
                    _messageService.ShowSuccess("Auto start added", $"\"{autoStart.Value}\" has been added to auto starts.");
                }
                catch (Exception e)
                {
                    var message = "Failed to revert remove";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        public void Enable(Guid id)
        {
            Task.Run(() =>
            {
                Logger.Info("{id} should be enabled", id);
                if (_autoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart))
                {
                    Enable(autoStart);
                }
                else
                {
                    var message = "Failed to get auto start to enable";
                    Logger.Error(message);
                    _messageService.ShowError(message);
                }
            });
        }

        public void Enable(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.Info("Should enable {@autoStart}", autoStart);
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    if (!_messageService.ShowConfirm("Confirm enable", $"Are you sure you want to enable auto start \"{autoStart.Value}\"?"))
                    {
                        return;
                    }
                    if (_autoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, EnableParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Enabled;
                    }
                    else
                    {
                        _autoStartService.EnableAutoStart(autoStart);
                    }
                    _messageService.ShowSuccess("Auto start enabled", $"\"{autoStart.Value}\" has been enabled.");
                }
                catch (Exception e)
                {
                    var message = "Failed to enable";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        public void Disable(Guid id)
        {
            Task.Run(() =>
            {
                Logger.Info("{id} should be disabled", id);
                if (_autoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart))
                {
                    Disable(autoStart);
                }
                else
                {
                    var message = "Failed to get auto start to disable";
                    Logger.Error(message);
                    _messageService.ShowError(message);
                }
            });
        }

        public void Disable(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.Info("Should disable {@autoStart}", autoStart);
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    if (!_messageService.ShowConfirm("Confirm disable", $"Are you sure you want to disable auto start \"{autoStart.Value}\"?"))
                    {
                        return;
                    }
                    if (_autoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, DisableParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Disabled;
                    }
                    else
                    {
                        _autoStartService.DisableAutoStart(autoStart);
                    }
                    _messageService.ShowSuccess("Auto start disabled", $"\"{autoStart.Value}\" has been disabled.");
                }
                catch (Exception e)
                {
                    var message = "Failed to disable";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmAdd(Guid id)
        {
            Task.Run(() =>
            {
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    Logger.Trace("ConfirmAdd called");
                    _autoStartService.ConfirmAdd(id);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm add of {id}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmAdd(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    Logger.Trace("ConfirmAdd called");
                    _autoStartService.ConfirmAdd(autoStart);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm add of {autoStart}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmRemove(Guid id)
        {
            Task.Run(() =>
            {
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    Logger.Trace("ConfirmRemove called");
                    _autoStartService.ConfirmRemove(id);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm remove of {id}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmRemove(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                try
                {
                    _appStatus.IncrementRunningActionCount();
                    Logger.Trace("ConfirmRemove called");
                    _autoStartService.ConfirmRemove(autoStart);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm remove of {autoStart}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    _messageService.ShowError(message, e);
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

        #region Event handlers
        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    // Listen to notification activation
        //    ToastNotificationManagerCompat.OnActivated += toastArgs => {
        //        Logger.Trace("Toast activated {Arguments}", toastArgs.Argument);
        //        // Obtain the arguments from the notification
        //        ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

        //        // Obtain any user input (text boxes, menu selections) from the notification
        //        ValueSet userInput = toastArgs.UserInput;

        //        // Need to dispatch to UI thread if performing UI operations
        //        Application.Current.Dispatcher.Invoke(delegate {
        //            Logger.Trace("Handling action {Arguments} {UserInput}", toastArgs.Argument, userInput);
        //            switch (args["action"])
        //            {
        //                case "viewRemove":
        //                    ShowRemoved(Guid.Parse(args["id"]));
        //                    break;
        //                case "revertRemove":
        //                    RevertRemove(Guid.Parse(args["id"]));
        //                    break;
        //                case "confirmRemove":
        //                    ConfirmRemove(Guid.Parse(args["id"]));
        //                    break;
        //                case "viewAdd":
        //                    ShowAdd(Guid.Parse(args["id"]));
        //                    break;
        //                case "revertAdd":
        //                    RevertAdd(Guid.Parse(args["id"]));
        //                    break;
        //                case "confirmAdd":
        //                    ConfirmAdd(Guid.Parse(args["id"]));
        //                    break;
        //                case "confirmEnable":
        //                    ConfirmAdd(Guid.Parse(args["id"]));
        //                    break;
        //                case "confirmDisable":
        //                    ConfirmAdd(Guid.Parse(args["id"]));
        //                    break;
        //                case "enable":
        //                    Enable(Guid.Parse(args["id"]));
        //                    break;
        //                case "disable":
        //                    Disable(Guid.Parse(args["id"]));
        //                    break;
        //                default:
        //                    Logger.Trace("Unknown action {Action}", args["action"]);
        //                    break;
        //            }
        //        });
        //    };
        //    base.OnStartup(e);
        //}

        private void AddHandler(AutoStartEntry addedAutostart)
        {
            Logger.Trace("AddHandler called");
            if (_autoStartService.IsOwnAutoStart(addedAutostart))
            {
                Logger.Info("Own auto start added");
                _appStatus.HasOwnAutoStart = true;
            }
            _notificationService.ShowNewAutoStartEntryNotification(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart)
        {
            Logger.Trace("RemoveHandler called");
            if (_autoStartService.IsOwnAutoStart(removedAutostart))
            {
                Logger.Info("Own auto start removed");
                _appStatus.HasOwnAutoStart = false;
            }
            _notificationService.ShowRemovedAutoStartEntryNotification(removedAutostart);
        }

        private void EnableHandler(AutoStartEntry enabledAutostart)
        {
            Logger.Trace("EnableHandler called");
            _notificationService.ShowEnabledAutoStartEntryNotification(enabledAutostart);
        }

        private void DisableHandler(AutoStartEntry disabledAutostart)
        {
            Logger.Trace("DisableHandler called");
            _notificationService.ShowDisabledAutoStartEntryNotification(disabledAutostart);
        }
        #endregion
    }
}
