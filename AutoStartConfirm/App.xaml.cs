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
using Windows.UI.ViewManagement;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Business;

namespace AutoStartConfirm
{
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application, IDisposable
    {
        private readonly ILogger<App> Logger;

        // delay creation until App.InitializeComponent has been called
        private MainWindow? Window = null;

        // delay creation until App.InitializeComponent has been called
        private NotifyIcon? notifyIcon = null;

        private NotifyIcon NotifyIcon
        {
            get
            {
                if (notifyIcon == null)
                {
                    notifyIcon = ServiceScope.ServiceProvider.GetRequiredService<NotifyIcon>();
                }
                return notifyIcon;
            }
        }

        private TaskbarIcon? TrayIcon;

        public IAppStatus AppStatus;

        private readonly IAutoStartBusiness AutoStartBusiness;

        private readonly INotificationService NotificationService;

        private readonly ISettingsService SettingsService;

        private readonly IUpdateService UpdateService;

        private readonly IDispatchService DispatchService;

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        private bool disposedValue;


        public App(
            ILogger<App> logger,
            IAppStatus appStatus,
            IAutoStartBusiness autoStartService,
            INotificationService notificationService,
            ISettingsService settingsService,
            IUpdateService updateService,
            IDispatchService dispatchService)
        {
            Logger = logger;
            AppStatus = appStatus;
            AutoStartBusiness = autoStartService;
            NotificationService = notificationService;
            SettingsService = settingsService;
            UpdateService = updateService;
            DispatchService = dispatchService;

            UnhandledException += UnhandledExceptionHandler;

            InitializeComponent();

        }

        private void UnhandledExceptionHandler(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Logger.LogCritical("Unhandled exception occurred: {eventArgs}", e);
        }

        public void Run()
        {
            Window = ServiceScope.ServiceProvider.GetRequiredService<MainWindow>();
            Window.Closed += WindowClosed;


            // disable notifications for new added auto starts on first start to avoid too many notifications at once
            bool isFirstRun = !AutoStartBusiness.GetValidAutoStartFileExists();
            if (!isFirstRun)
            {
                AutoStartBusiness.Add += AddHandler;
                AutoStartBusiness.Remove += RemoveHandler;
                AutoStartBusiness.Enable += EnableHandler;
                AutoStartBusiness.Disable += DisableHandler;
            }

            try
            {
                AutoStartBusiness.LoadCurrentAutoStarts();
                AppStatus.HasOwnAutoStart = AutoStartBusiness.HasOwnAutoStart;
            }
            catch (Exception)
            {
            }

            if (isFirstRun)
            {
                AutoStartBusiness.Add += AddHandler;
                AutoStartBusiness.Remove += RemoveHandler;
                AutoStartBusiness.Enable += EnableHandler;
                AutoStartBusiness.Disable += DisableHandler;
            }
            AutoStartBusiness.StartWatcher();

            if (SettingsService.CheckForUpdatesOnStart)
            {
                UpdateService.CheckUpdateAndShowNotification();
            }
        }

        /// <summary>
        /// Handles auto starts if command line parameters are set
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns>True, if parameters were set, correctly handled and the program can be closed</returns>
        public bool HandleCommandLineParameters(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (string.Equals(arg, AutoStartBusiness.RevertAddParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Adding should be reverted");
                        AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                        AutoStartBusiness.RemoveAutoStart(autoStartEntry, false);
                        Logger.LogInformation("Finished");
                        return true;
                    }
                    else if (string.Equals(arg, AutoStartBusiness.RevertRemoveParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Removing should be reverted");
                        AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                        AutoStartBusiness.AddAutoStart(autoStartEntry, false);
                        Logger.LogInformation("Finished");
                        return true;
                    }
                    else if (string.Equals(arg, AutoStartBusiness.EnableParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Auto start should be enabled");
                        AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                        AutoStartBusiness.EnableAutoStart(autoStartEntry, false);
                        Logger.LogInformation("Finished");
                        return true;
                    }
                    else if (string.Equals(arg, AutoStartBusiness.DisableParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Auto start should be disabled");
                        AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                        AutoStartBusiness.DisableAutoStart(autoStartEntry, false);
                        Logger.LogInformation("Finished");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to handle command line parameters");
                throw;
            }
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
            using Stream stream = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                XmlSerializer serializer = new(typeof(AutoStartEntry));
                var ret = (AutoStartEntry?)serializer.Deserialize(stream) ?? throw new InvalidDataException("File is empty");
                return ret;
            }
            catch (Exception ex)
            {
                var err = new Exception("Failed to deserialize", ex);
                throw err;
            }
        }

        private void ExitHandler(object? sender, EventArgs args)
        {
            Exit();
        }

        public void Exit(int errorCode = 0)
        {
            TrayIcon?.Dispose();
            TrayIcon = null;
            AutoStartBusiness.StopWatcher();
            ServiceScope.Dispose();
            base.Exit();
            Dispose();
            System.Environment.Exit(errorCode);
        }

        public void ToggleMainWindow()
        {
            Logger.LogTrace("Toggling main window");
            if (Window == null)
            {
                return;
            }
            if (Window.Visible)
            {
                Logger.LogTrace("Hiding main window");
                Window.Hide();
            }
            else
            {
                Logger.LogTrace("Showing main window");
                Window.Show();
            }
        }

        public void ViewUpdate()
        {
            Logger.LogInformation("Viewing update");
            Process.Start("https://github.com/ChristianGalla/AutoStartConfirm/releases");
        }

        public void InstallUpdate(string? msiUrl = null)
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

        #region Event handlers
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            NotifyIcon.Exit += ExitHandler;
            NotifyIcon.ToggleMainWindow += ToggleMainWindowHandler;
            TrayIcon = (TaskbarIcon)NotifyIcon["TrayIcon"];
            TrayIcon.ForceCreate();

            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                Logger.LogTrace("Toast activated {Arguments}", toastArgs.Argument);
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);

                // Obtain any user input (text boxes, menu selections) from the notification
                ValueSet userInput = toastArgs.UserInput;

                // Need to dispatch to UI thread if performing UI operations
                DispatchService.DispatcherQueue.TryEnqueue(() => {
                    Logger.LogTrace("Handling action {Arguments} {UserInput}", toastArgs.Argument, userInput);
                    if (args.TryGetValue("action", out string? action))
                    {
                        switch (action)
                        {
                            case "viewRemove":
                                // todo: implement ShowRemoved
                                // AutoStartBusiness.ShowRemoved(Guid.Parse(args["id"]));
                                break;
                            case "revertRemove":
                                AutoStartBusiness.AddAutoStart(Guid.Parse(args["id"]));
                                break;
                            case "confirmRemove":
                                AutoStartBusiness.ConfirmRemove(Guid.Parse(args["id"]));
                                break;
                            case "viewAdd":
                                // todo: implement ShowAdd
                                // AutoStartBusiness.ShowAdd(Guid.Parse(args["id"]));
                                break;
                            case "revertAdd":
                                AutoStartBusiness.RemoveAutoStart(Guid.Parse(args["id"]));
                                break;
                            case "confirmAdd":
                                AutoStartBusiness.ConfirmAdd(Guid.Parse(args["id"]));
                                break;
                            case "confirmEnable":
                                AutoStartBusiness.ConfirmAdd(Guid.Parse(args["id"]));
                                break;
                            case "confirmDisable":
                                AutoStartBusiness.ConfirmAdd(Guid.Parse(args["id"]));
                                break;
                            case "enable":
                                AutoStartBusiness.EnableAutoStart(Guid.Parse(args["id"]));
                                break;
                            case "viewUpdate":
                                ViewUpdate();
                                break;
                            case "disable":
                                AutoStartBusiness.DisableAutoStart(Guid.Parse(args["id"]));
                                break;
                            case "installUpdate":
                                InstallUpdate(args["msiUrl"]);
                                break;
                            default:
                                Logger.LogTrace("Unknown action {Action}", action);
                                break;
                        }
                    }
                    else
                    {
                        Logger.LogTrace("Missing action");
                    }
                });
            };
        }

        private void ToggleMainWindowHandler(object? sender, EventArgs e)
        {
            ToggleMainWindow();
        }

        private void AddHandler(AutoStartEntry addedAutostart)
        {
            Logger.LogTrace("AddHandler called");
            if (AutoStartBusiness.IsOwnAutoStart(addedAutostart))
            {
                Logger.LogInformation("Own auto start added");
                AppStatus.HasOwnAutoStart = true;
            }
            NotificationService.ShowNewAutoStartEntryNotification(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart)
        {
            Logger.LogTrace("RemoveHandler called");
            if (AutoStartBusiness.IsOwnAutoStart(removedAutostart))
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

        private void WindowClosed(object sender, WindowEventArgs args)
        {
            args.Handled = true;
            Window?.Hide();
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
