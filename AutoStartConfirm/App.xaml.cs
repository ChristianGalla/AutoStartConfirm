using AutoStartConfirm.Business;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Foundation.Collections;

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

        private readonly IDispatchService DispatchService;

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        private bool disposedValue;


        public App(
            ILogger<App> logger,
            IAppStatus appStatus,
            IAutoStartBusiness autoStartService,
            IDispatchService dispatchService)
        {
            Logger = logger;
            AppStatus = appStatus;
            AutoStartBusiness = autoStartService;
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

            AutoStartBusiness.Run();
        }

        /// <summary>
        /// Handles auto starts if command line parameters are set
        /// </summary>
        /// <param name="args">Command line parameters</param>
        /// <returns>True, if parameters were set, correctly handled and the program can be closed</returns>
        public async Task<bool> HandleCommandLineParameters(string[] args)
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
                        await AutoStartBusiness.RemoveAutoStart(autoStartEntry, false);
                        Logger.LogInformation("Finished");
                        return true;
                    }
                    else if (string.Equals(arg, AutoStartBusiness.RevertRemoveParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Removing should be reverted");
                        AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                        await AutoStartBusiness.AddAutoStart(autoStartEntry, false);
                        Logger.LogInformation("Finished");
                        return true;
                    }
                    else if (string.Equals(arg, AutoStartBusiness.EnableParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Auto start should be enabled");
                        AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                        await AutoStartBusiness.EnableAutoStart(autoStartEntry, false);
                        Logger.LogInformation("Finished");
                        return true;
                    }
                    else if (string.Equals(arg, AutoStartBusiness.DisableParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInformation("Auto start should be disabled");
                        AutoStartEntry autoStartEntry = LoadAutoStartFromParameter(args, i);
                        await AutoStartBusiness.DisableAutoStart(autoStartEntry, false);
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
        protected override void OnLaunched(LaunchActivatedEventArgs args)
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
                DispatchService.TryEnqueue(() =>
                {
                    Logger.LogTrace("Handling action {Arguments} {UserInput}", toastArgs.Argument, userInput);
                    if (args.TryGetValue("action", out string? action))
                    {
                        switch (action)
                        {
                            case "viewRemove":
                                // todo: implement ShowRemoved
                                // AutoStartBusiness.ShowRemoved(Guid.Parse(args["id"]));
                                Window?.Show();
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
                                Window?.Show();
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
                        Window?.Show();
                    }
                });
            };
        }

        private void ToggleMainWindowHandler(object? sender, EventArgs e)
        {
            ToggleMainWindow();
        }

        private void WindowClosed(object sender, WindowEventArgs args)
        {
            args.Handled = true;
            Window?.Hide();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceScope.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
