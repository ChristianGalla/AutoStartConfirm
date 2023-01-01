using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Foundation.Collections;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Update;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using H.NotifyIcon;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AutoStartConfirm
{
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private static TaskbarIcon NotifyIcon;

        private readonly ILogger<App> Logger;

        // delay Window creation until App.InitializeComponent has been called
        private MainWindow window = null;

        private MainWindow Window {
            get {
                if (window == null)
                {
                    window = ServiceScope.ServiceProvider.GetRequiredService<MainWindow>();
                }
                return window;
            }
        }

        public IAppStatus AppStatus;

        private readonly IAutoStartService AutoStartService;

        private readonly INotificationService NotificationService;

        private readonly IMessageService MessageService;

        private readonly ISettingsService SettingsService;

        private readonly IUpdateService UpdateService;

        private static readonly string RevertAddParameterName = "--revertAdd";

        private static readonly string RevertRemoveParameterName = "--revertRemove";

        private static readonly string EnableParameterName = "--enable";

        private static readonly string DisableParameterName = "--disable";

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        private bool disposedValue;


        public App(
            ILogger<App> logger,
            IAppStatus appStatus,
            IAutoStartService autoStartService,
            INotificationService notificationService,
            IMessageService messageService,
            ISettingsService settingsService,
            IUpdateService updateService)
        {
            Logger = logger;
            AppStatus = appStatus;
            AutoStartService = autoStartService;
            NotificationService = notificationService;
            MessageService = messageService;
            SettingsService = settingsService;
            UpdateService = updateService;

            this.InitializeComponent();

            // disable notifications for new added auto starts on first start to avoid too many notifications at once
            bool isFirstRun = !AutoStartService.GetValidAutoStartFileExists();
            if (!isFirstRun)
            {
                AutoStartService.Add += AddHandler;
                AutoStartService.Remove += RemoveHandler;
                AutoStartService.Enable += EnableHandler;
                AutoStartService.Disable += DisableHandler;
            }

            try
            {
                AutoStartService.LoadCurrentAutoStarts();
                AppStatus.HasOwnAutoStart = AutoStartService.HasOwnAutoStart;
            }
            catch (Exception)
            {
            }

            if (isFirstRun)
            {
                AutoStartService.Add += AddHandler;
                AutoStartService.Remove += RemoveHandler;
                AutoStartService.Enable += EnableHandler;
                AutoStartService.Disable += DisableHandler;
            }
            AutoStartService.StartWatcher();

            if (SettingsService.CheckForUpdatesOnStart)
            {
                UpdateService.CheckUpdateAndShowNotification();
            }

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            //NotifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            //NotifyIcon.ForceCreate();
        }

        private void RevertRemoveIdHandler(Guid e)
        {
            RevertRemove(e);
        }

        private void RevertRemoveHandler(AutoStartEntry e)
        {
            RevertRemove(e);
        }

        private void ConfirmRemoveHandler(AutoStartEntry e)
        {
            ConfirmRemove(e);
        }

        private void RevertAddIdHandler(Guid e)
        {
            RevertAdd(e);
        }

        private void RevertAddHandler(AutoStartEntry e)
        {
            RevertAddHandler(e);
        }

        private void ConfirmAddHandler(AutoStartEntry e)
        {
            ConfirmAdd(e);
        }

        private void ToggleOwnAutoStartHandler(object sender, EventArgs e)
        {
            ToggleOwnAutoStart();
        }

        public Task ToggleOwnAutoStart()
        {
            return Task.Run(() =>
            {
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    AutoStartService.ToggleOwnAutoStart();
                }
                catch (Exception e)
                {
                    var message = "Failed to change own auto start";
#pragma warning disable CA2254 // Template should be a static expression
                    Logger.LogError(e, message);
#pragma warning restore CA2254 // Template should be a static expression
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        /// <summary>
        /// Handles auto starts if command line parameters are set
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns>True, if parameters were set, correctly handled and the program can be closed</returns>
        public bool HandleCommandLineParameters(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, RevertAddParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation("Adding should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.RemoveAutoStart(autoStartEntry);
                    Logger.LogInformation("Finished");
                    return true;
                }
                else if (string.Equals(arg, RevertRemoveParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation("Removing should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.AddAutoStart(autoStartEntry);
                    Logger.LogInformation("Finished");
                    return true;
                }
                else if (string.Equals(arg, EnableParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation("Auto start should be enabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.EnableAutoStart(autoStartEntry);
                    Logger.LogInformation("Finished");
                    return true;
                }
                else if (string.Equals(arg, DisableParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation("Auto start should be disabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.DisableAutoStart(autoStartEntry);
                    Logger.LogInformation("Finished");
                    return true;
                }
            }
            return false;
        }

        private AutoStartEntry LoadAutoStartFromParameter(string[] args, int i)
        {
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException("Missing path to file");
            }
            var path = args[i + 1];
            var autoStartEntry = LoadAutoStartFromFile(path);
            return autoStartEntry;
        }

        private AutoStartEntry LoadAutoStartFromFile(string path)
        {
            Logger.LogTrace("LoadAutoStartFromFile called");
            using (Stream stream = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AutoStartEntry));
                    var ret = (AutoStartEntry)serializer.Deserialize(stream);
                    return ret;
                }
                catch (Exception ex)
                {
                    var err = new Exception("Failed to deserialize", ex);
                    throw err;
                }
            }
        }

        private void ExitHandler(object sender, EventArgs e)
        {
            // Close();
        }

        private void OwnAutoStartToggleHandler(object sender, EventArgs e)
        {
            ToggleOwnAutoStart();
        }

        private void OpenHandler(object sender, EventArgs e)
        {
            // MainWindow.Show();
        }

        public void ToggleMainWindow()
        {
            Logger.LogTrace("Toggling main window");
            //if (Window.IsVisible)
            //{
            //    Logger.LogTrace("Closing main window");
            //    Window.Hide();
            //}
            //else
            //{
            //    Logger.LogTrace("Showing main window");
            //    Window.Show();
            //}
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void ShowAdd(Guid id)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // todo: jump to added
            Logger.LogTrace("ShowAdd called");
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public void ShowRemoved(Guid id)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            // todo: jump to removed
            Logger.LogTrace("ShowRemoved called");
        }

        public void RevertAdd(Guid id)
        {
            Logger.LogInformation("Addition of {id} should be reverted", id);
            if (AutoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart))
            {
                RevertAdd(autoStart);
            }
            else
            {
                var message = "Failed to get auto start to remove";
                Logger.LogError("Failed to get auto start {id} to remove", id);
                MessageService.ShowError(message);
            }
        }

        public void RevertAdd(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.LogInformation("Should add {@autoStart}", autoStart);
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    if (!MessageService.ShowConfirm("Confirm remove", $"Are you sure you want to remove \"{autoStart.Value}\" from auto starts?"))
                    {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, RevertAddParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    }
                    else
                    {
                        AutoStartService.RemoveAutoStart(autoStart);
                    }
                    MessageService.ShowSuccess("Auto start removed", $"\"{autoStart.Value}\" has been removed from auto starts.");
                }
                catch (Exception e)
                {
                    var message = "Failed to revert add";
                    Logger.LogError(e, "Failed to revert add of {@autoStart}", autoStart);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        private void StartSubProcessAsAdmin(AutoStartEntry autoStart, string parameterName)
        {
            Logger.LogTrace("StartSubProcessAsAdmin called");
            string path = Path.GetTempFileName();
            try
            {
                using (Stream stream = new FileStream($"{path}", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AutoStartEntry));
                    serializer.Serialize(stream, autoStart);
                }

                var info = new ProcessStartInfo(
                    AutoStartService.CurrentExePath,
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
            Logger.LogInformation("Removal of {id} should be reverted", id);
            if (AutoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart))
            {
                RevertRemove(autoStart);
            }
            else
            {
                var message = "Failed to get auto start to add";
                Logger.LogError("Failed to get auto start {id} to add", id);
                MessageService.ShowError(message);
            }
        }

        public void RevertRemove(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.LogInformation("Should remove {@autoStart}", autoStart);
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    if (!MessageService.ShowConfirm("Confirm add", $"Are you sure you want to add \"{autoStart.Value}\" as auto start?"))
                    {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, RevertRemoveParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                    }
                    else
                    {
                        AutoStartService.AddAutoStart(autoStart);
                    }
                    MessageService.ShowSuccess("Auto start added", $"\"{autoStart.Value}\" has been added to auto starts.");
                }
                catch (Exception e)
                {
                    var message = "Failed to revert remove";
                    Logger.LogError(e, "Failed to revert remove of {@autoStart}", autoStart);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        public void Enable(Guid id)
        {
            Task.Run(() =>
            {
                Logger.LogInformation("{id} should be enabled", id);
                if (AutoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart))
                {
                    Enable(autoStart);
                }
                else
                {
                    var message = "Failed to get auto start to enable";
                    Logger.LogError("Failed to get auto start {id} to enable", id);
                    MessageService.ShowError(message);
                }
            });
        }

        public void Enable(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.LogInformation("Should enable {@autoStart}", autoStart);
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    if (!MessageService.ShowConfirm("Confirm enable", $"Are you sure you want to enable auto start \"{autoStart.Value}\"?"))
                    {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, EnableParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Enabled;
                    }
                    else
                    {
                        AutoStartService.EnableAutoStart(autoStart);
                    }
                    MessageService.ShowSuccess("Auto start enabled", $"\"{autoStart.Value}\" has been enabled.");
                }
                catch (Exception e)
                {
                    var message = "Failed to enable";
                    Logger.LogError(e, "Failed to enable {@autoStart}", autoStart);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        public void Disable(Guid id)
        {
            Task.Run(() =>
            {
                Logger.LogInformation("{id} should be disabled", id);
                if (AutoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart))
                {
                    Disable(autoStart);
                }
                else
                {
                    var message = "Failed to get auto start to disable";
                    Logger.LogError("Failed to get auto start {id} to disable", id);
                    MessageService.ShowError(message);
                }
            });
        }

        public void Disable(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.LogInformation("Should disable {@autoStart}", autoStart);
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    if (!MessageService.ShowConfirm("Confirm disable", $"Are you sure you want to disable auto start \"{autoStart.Value}\"?"))
                    {
                        return;
                    }
                    if (AutoStartService.IsAdminRequiredForChanges(autoStart))
                    {
                        StartSubProcessAsAdmin(autoStart, DisableParameterName);
                        autoStart.ConfirmStatus = ConfirmStatus.Disabled;
                    }
                    else
                    {
                        AutoStartService.DisableAutoStart(autoStart);
                    }
                    MessageService.ShowSuccess("Auto start disabled", $"\"{autoStart.Value}\" has been disabled.");
                }
                catch (Exception e)
                {
                    var message = "Failed to disable";
                    Logger.LogError(e, "Failed to disable {@autoStart}", autoStart);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmAdd(Guid id)
        {
            Task.Run(() =>
            {
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    Logger.LogTrace("ConfirmAdd called");
                    AutoStartService.ConfirmAdd(id);
                }
                catch (Exception e)
                {
                    var message = "Failed to confirm add";
                    Logger.LogError(e, "Failed to confirm add of {id}", id);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmAdd(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    Logger.LogTrace("ConfirmAdd called");
                    AutoStartService.ConfirmAdd(autoStart);
                }
                catch (Exception e)
                {
                    var message = "Failed to confirm add";
                    Logger.LogError(e, "Failed to confirm add of {@autoStart}", autoStart);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmRemove(Guid id)
        {
            Task.Run(() =>
            {
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    Logger.LogTrace("ConfirmRemove called");
                    AutoStartService.ConfirmRemove(id);
                }
                catch (Exception e)
                {
                    var message = "Failed to confirm remove";
                    Logger.LogError(e, "Failed to confirm remove of {id}", id);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ConfirmRemove(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    Logger.LogTrace("ConfirmRemove called");
                    AutoStartService.ConfirmRemove(autoStart);
                }
                catch (Exception e)
                {
                    var message = "Failed to confirm remove";
                    Logger.LogError(e, "Failed to confirm remove of {@autoStart}", autoStart);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        public void ViewUpdate()
        {
            Logger.LogInformation("Viewing update");
            Process.Start("https://github.com/ChristianGalla/AutoStartConfirm/releases");
        }

        public void InstallUpdate(string msiUrl = null)
        {
            if (msiUrl == null || msiUrl.Length == 0)
            {
                Logger.LogError("msi URL missing");
                return;
            }
            Process.Start("Msiexec.exe", $"/i \"{msiUrl}\" /qb+");
        }


        private void ExitClicked(object sender, EventArgs e)
        {
            // Close();
        }

        private void OwnAutoStartClicked(object sender, EventArgs e)
        {
            ToggleOwnAutoStart();
        }

        private void IconDoubleClicked(object sender, RoutedEventArgs e)
        {
            ToggleMainWindow();
        }

        #region Event handlers
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            //// Listen to notification activation
            //ToastNotificationManagerCompat.OnActivated += toastArgs =>
            //{
            //    Logger.LogTrace("Toast activated {Arguments}", toastArgs.Argument);
            //    // Obtain the arguments from the notification
            //    ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

            //    // Obtain any user input (text boxes, menu selections) from the notification
            //    ValueSet userInput = toastArgs.UserInput;

            //    Need to dispatch to UI thread if performing UI operations
            //    System.Windows.Application.Current.Dispatcher.Invoke(delegate
            //    {
            //        Logger.LogTrace("Handling action {Arguments} {UserInput}", toastArgs.Argument, userInput);
            //        if (args.TryGetValue("action", out string action))
            //        {
            //            switch (action)
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
            //                case "viewUpdate":
            //                    ViewUpdate();
            //                    break;
            //                case "disable":
            //                    Disable(Guid.Parse(args["id"]));
            //                    break;
            //                case "installUpdate":
            //                    InstallUpdate(args["msiUrl"]);
            //                    break;
            //                default:
            //                    Logger.LogTrace("Unknown action {Action}", action);
            //                    break;
            //            }
            //        }
            //        else
            //        {
            //            Logger.LogTrace("Missing action");
            //        }
            //    });
            //};

            Window.ConfirmAdd += ConfirmAddHandler;
            Window.RevertAdd += RevertAddHandler;
            Window.RevertAddId += RevertAddIdHandler;
            Window.Enable += EnableHandler;
            Window.Disable += DisableHandler;
            Window.ConfirmRemove += ConfirmRemoveHandler;
            Window.RevertRemove += RevertRemoveHandler;
            Window.RevertRemoveId += RevertRemoveIdHandler;
            Window.ToggleOwnAutoStart += ToggleOwnAutoStartHandler;
        }

        private void AddHandler(AutoStartEntry addedAutostart)
        {
            Logger.LogTrace("AddHandler called");
            if (AutoStartService.IsOwnAutoStart(addedAutostart))
            {
                Logger.LogInformation("Own auto start added");
                AppStatus.HasOwnAutoStart = true;
            }
            NotificationService.ShowNewAutoStartEntryNotification(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart)
        {
            Logger.LogTrace("RemoveHandler called");
            if (AutoStartService.IsOwnAutoStart(removedAutostart))
            {
                Logger.LogInformation("Own auto start removed");
                AppStatus.HasOwnAutoStart = false;
            }
            NotificationService.ShowRemovedAutoStartEntryNotification(removedAutostart);
        }

        private void EnableHandler(AutoStartEntry enabledAutostart)
        {
            Logger.LogTrace("EnableHandler called");
            NotificationService.ShowEnabledAutoStartEntryNotification(enabledAutostart);
        }

        private void DisableHandler(AutoStartEntry disabledAutostart)
        {
            Logger.LogTrace("DisableHandler called");
            NotificationService.ShowDisabledAutoStartEntryNotification(disabledAutostart);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceScope.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~App()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
