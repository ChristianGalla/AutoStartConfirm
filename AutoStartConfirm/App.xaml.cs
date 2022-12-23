using AutoStartConfirm.Connectors;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Models;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using Windows.Foundation.Collections;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Update;
using System.ServiceModel.Channels;
using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Net.NetworkInformation;
using AutoStartConfirm.Connectors.Registry;

namespace AutoStartConfirm
{
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : System.Windows.Application, IDisposable, IApp
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static TaskbarIcon Icon = null;

        public static ServiceProvider ServiceProvider;

        private readonly MainWindow Window;

        private readonly IAppStatus AppStatus;

        private readonly IAutoStartService AutoStartService;

        private readonly INotificationService NotificationService;

        private readonly IMessageService MessageService;

        private readonly ISettingsService SettingsService;

        private readonly IUpdateService UpdateService;

        private static readonly string RevertAddParameterName = "--revertAdd";

        private static readonly string RevertRemoveParameterName = "--revertRemove";

        private static readonly string EnableParameterName = "--enable";

        private static readonly string DisableParameterName = "--disable";

        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<MainWindow>()
                .AddSingleton<ConnectorWindow>()
                .AddSingleton<AboutWindow>()
                .AddSingleton<IApp, App>()
                .AddSingleton<IAppStatus, AppStatus>()
                .AddSingleton<IAutoStartService, AutoStartService>()
                .AddSingleton<INotificationService, NotificationService>()
                .AddSingleton<IMessageService, MessageService>()
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<IUpdateService, UpdateService>()
                .AddSingleton<IAutoStartConnectorService, AutoStartConnectorService>();
            AutoStartConnectorService.ConfigureServices(services);
        }


        public App(MainWindow window, IAppStatus appStatus, IAutoStartService autoStartService, INotificationService notificationService, IMessageService messageService, ISettingsService settingsService, IUpdateService updateService)
        {
            Window = window;
            AppStatus = appStatus;
            AutoStartService = autoStartService;
            NotificationService = notificationService;
            MessageService = messageService;
            SettingsService = settingsService;
            UpdateService = updateService;
            SettingsService.EnsureConfiguration();
        }

        public void Start(bool skipInitializing = false)
        {
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

            if (!skipInitializing)
            {
                InitializeComponent();
            }
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
                    var err = new Exception(message, e);
                    Logger.Error(err);
                    MessageService.ShowError(message, e);
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        public static int Main(string[] args)
        {
            try
            {
                ServiceCollection services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();
                using (var serviceScope = ServiceProvider.CreateScope())
                {
                    Logger.Info("Starting");
                    IApp app = serviceScope.ServiceProvider.GetRequiredService<IApp>();
                    Logger.Info("Parameters: {args}", args);
                    if (app.HandleCommandLineParameters(args))
                    {
                        return 0;
                    }
                    Logger.Info("Normal start");
                    app.Start();
                    app.Run(); // blocks until program is closing
                    Logger.Info("Finished");
                }
                return 0;
            }
            catch (Exception e)
            {
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
        public bool HandleCommandLineParameters(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, RevertAddParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Adding should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.RemoveAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
                else if (string.Equals(arg, RevertRemoveParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Removing should be reverted");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.AddAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
                else if (string.Equals(arg, EnableParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Auto start should be enabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.EnableAutoStart(autoStartEntry);
                    Logger.Info("Finished");
                    return true;
                }
                else if (string.Equals(arg, DisableParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Auto start should be disabled");
                    AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                    AutoStartService.DisableAutoStart(autoStartEntry);
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

        void App_Startup(object sender, StartupEventArgs e)
        {
            Logger.Trace("App_Startup called");
            Icon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        public void ToggleMainWindow()
        {
            Logger.Trace("Toggling main window");
            if (Window.IsVisible)
            {
                Logger.Trace("Closing main window");
                Window.Close();
            }
            else
            {
                Logger.Trace("Showing main window");
                Window.Show();
            }
        }

        internal static void Close()
        {
            Logger.Info("Closing application");
            try
            {
                Current.Shutdown();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                AutoStartService.SaveAutoStarts();
            }
            finally
            {
                try
                {
                    Icon.Dispose();
                }
                catch (Exception)
                {
                }
                base.OnExit(e);
            }
        }

        public void ShowAdd(Guid id)
        {
            // todo: jump to added
            Logger.Trace("ShowAdd called");
        }

        public void ShowRemoved(Guid id)
        {
            // todo: jump to removed
            Logger.Trace("ShowRemoved called");
        }

        public void RevertAdd(Guid id)
        {
            Logger.Info("Addition of {id} should be reverted", id);
            if (AutoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart))
            {
                RevertAdd(autoStart);
            }
            else
            {
                var message = "Failed to get auto start to remove";
                Logger.Error(message);
                MessageService.ShowError(message);
            }
        }

        public void RevertAdd(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.Info("Should add {@autoStart}", autoStart);
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
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
            Logger.Trace("StartSubProcessAsAdmin called");
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
            Logger.Info("Removal of {id} should be reverted", id);
            if (AutoStartService.TryGetHistoryAutoStart(id, out AutoStartEntry autoStart))
            {
                RevertRemove(autoStart);
            }
            else
            {
                var message = "Failed to get auto start to add";
                Logger.Error(message);
                MessageService.ShowError(message);
            }
        }

        public void RevertRemove(AutoStartEntry autoStart)
        {
            Task.Run(() =>
            {
                Logger.Info("Should remove {@autoStart}", autoStart);
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
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
                Logger.Info("{id} should be enabled", id);
                if (AutoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart))
                {
                    Enable(autoStart);
                }
                else
                {
                    var message = "Failed to get auto start to enable";
                    Logger.Error(message);
                    MessageService.ShowError(message);
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
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
                Logger.Info("{id} should be disabled", id);
                if (AutoStartService.TryGetCurrentAutoStart(id, out AutoStartEntry autoStart))
                {
                    Disable(autoStart);
                }
                else
                {
                    var message = "Failed to get auto start to disable";
                    Logger.Error(message);
                    MessageService.ShowError(message);
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
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
                    Logger.Trace("ConfirmAdd called");
                    AutoStartService.ConfirmAdd(id);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm add of {id}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
                    Logger.Trace("ConfirmAdd called");
                    AutoStartService.ConfirmAdd(autoStart);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm add of {autoStart}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
                    Logger.Trace("ConfirmRemove called");
                    AutoStartService.ConfirmRemove(id);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm remove of {id}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
                    Logger.Trace("ConfirmRemove called");
                    AutoStartService.ConfirmRemove(autoStart);
                }
                catch (Exception e)
                {
                    var message = $"Failed to confirm remove of {autoStart}";
                    var err = new Exception(message, e);
                    Logger.Error(err);
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
            Process.Start("https://github.com/ChristianGalla/AutoStartConfirm/releases");
        }

        public void InstallUpdate(string msiUrl = null)
        {
            if (msiUrl == null || msiUrl.Length == 0)
            {
                var err = new Exception("msi URL missing");
                Logger.Error(err);
                return;
            }
            Process.Start("Msiexec.exe", $"/i \"{msiUrl}\" /qb+");
        }

        #region Event handlers
        protected override void OnStartup(StartupEventArgs e)
        {
            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                Logger.Trace("Toast activated {Arguments}", toastArgs.Argument);
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;

                // Need to dispatch to UI thread if performing UI operations
                System.Windows.Application.Current.Dispatcher.Invoke(delegate
                {
                    Logger.Trace("Handling action {Arguments} {UserInput}", toastArgs.Argument, userInput);
                    if (args.TryGetValue("action", out string action))
                    {
                        switch (action)
                        {
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
                            case "viewUpdate":
                                ViewUpdate();
                                break;
                            case "disable":
                                Disable(Guid.Parse(args["id"]));
                                break;
                            case "installUpdate":
                                InstallUpdate(args["msiUrl"]);
                                break;
                            default:
                                Logger.Trace("Unknown action {Action}", action);
                                break;
                        }
                    }
                    else
                    {
                        Logger.Trace("Missing action");
                    }
                });
            };
            base.OnStartup(e);
        }

        private void AddHandler(AutoStartEntry addedAutostart)
        {
            Logger.Trace("AddHandler called");
            if (AutoStartService.IsOwnAutoStart(addedAutostart))
            {
                Logger.Info("Own auto start added");
                AppStatus.HasOwnAutoStart = true;
            }
            NotificationService.ShowNewAutoStartEntryNotification(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart)
        {
            Logger.Trace("RemoveHandler called");
            if (AutoStartService.IsOwnAutoStart(removedAutostart))
            {
                Logger.Info("Own auto start removed");
                AppStatus.HasOwnAutoStart = false;
            }
            NotificationService.ShowRemovedAutoStartEntryNotification(removedAutostart);
        }

        private void EnableHandler(AutoStartEntry enabledAutostart)
        {
            Logger.Trace("EnableHandler called");
            NotificationService.ShowEnabledAutoStartEntryNotification(enabledAutostart);
        }

        private void DisableHandler(AutoStartEntry disabledAutostart)
        {
            Logger.Trace("DisableHandler called");
            NotificationService.ShowDisabledAutoStartEntryNotification(disabledAutostart);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceProvider.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
