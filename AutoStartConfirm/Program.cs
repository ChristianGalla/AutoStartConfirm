using AutoStartConfirm.Business;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Update;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using NLog;
using NLog.Web;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace AutoStartConfirm
{
    public static class Program
    {
        private static int activationCount = 1;
        public static List<string> OutputStack { get; private set; } = new();

        private static ServiceProvider? ServiceProvider;

        // Replaces the standard App.g.i.cs.
        // Note: We can't declare Main to be async because in a WinUI app
        // this prevents Narrator from reading XAML elements.
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();

                bool isRedirect = DecideRedirection();
                if (!isRedirect)
                {
                    Microsoft.UI.Xaml.Application.Start(async (p) =>
                    {
                        var context = new DispatcherQueueSynchronizationContext(
                            DispatcherQueue.GetForCurrentThread());
                        SynchronizationContext.SetSynchronizationContext(context);
                        ServiceCollection services = new();
                        ConfigureServices(services);
                        ServiceProvider = services.BuildServiceProvider();
                        Ioc.Default.ConfigureServices(ServiceProvider);
                        using var serviceScope = ServiceProvider.CreateScope();
                        var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<App>>();
                        _ = serviceScope.ServiceProvider.GetRequiredService<IDispatchService>();
                        try
                        {
                            logger.LogInformation("Starting");
                            App app = serviceScope.ServiceProvider.GetRequiredService<App>();
                            logger.LogInformation("Parameters: {args}", args);
                            if (await app.HandleCommandLineParameters(args))
                            {
                                app.Exit();
                            }
                            logger.LogInformation("Normal start");
                            app.Run();
                        }
                        catch (Exception e)
                        {
                            logger.LogCritical(e, "Failed to run");
                            throw;
                        }
                    });
                }
                return 0;
            }
            catch (Exception)
            {
                return 2;
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            
            services
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLogWeb((IServiceProvider serviceProvider) =>
                    {
                        var nlogPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "nlog.config");
                        var logFactory = new NLog.LogFactory();
                        logFactory.Setup().LoadConfigurationFromFile(nlogPath);
                        logFactory.ServiceRepository.RegisterService(typeof(IJsonConverter), new JsonNetSerializer());
                        return logFactory;
                    });
                })
                .AddSingleton<IUacService, UacService>()
                .AddSingleton<IDispatchService, DispatchService>()
                .AddSingleton<App>()
                .AddSingleton<MainWindow>()
                .AddSingleton<NotifyIcon>()
                .AddSingleton<IAppStatus, AppStatus>()
                .AddSingleton<IAutoStartBusiness, AutoStartBusiness>()
                .AddSingleton<INotificationService, NotificationService>()
                .AddSingleton<IMessageService, MessageService>()
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<IUpdateService, UpdateService>()
                .AddSingleton<IGitHubClient>(ServiceProvider => {
                    return new GitHubClient(new ProductHeaderValue("AutoStartConfirm"));
                });
            AutoStartConnectorService.ConfigureServices(services);
        }

        #region Report helpers

        public static void ReportInfo(string _)
        {
            // If we already have a form, display the message now.
            // Otherwise, add it to the collection for displaying later.
            //if (App.Current is App thisApp && thisApp.AppWindow != null
            //    && thisApp.AppWindow is MainWindow mainWindow)
            //{
            //    mainWindow.OutputMessage(message);
            //}
            //else
            //{
            //    OutputStack.Add(message);
            //}
        }

        private static void ReportFileArgs(string callSite, AppActivationArguments args)
        {
            ReportInfo($"called from {callSite}");
            if (args.Data is IFileActivatedEventArgs fileArgs)
            {
                IStorageItem? item = fileArgs.Files.ElementAtOrDefault(0);
                if (item is StorageFile file)
                {
                    ReportInfo($"file: {file.Name}");
                }
            }
        }

        private static void ReportLaunchArgs(string callSite, AppActivationArguments args)
        {
            ReportInfo($"called from {callSite}");
            if (args.Data is ILaunchActivatedEventArgs launchArgs)
            {
                string[] argStrings = launchArgs.Arguments.Split();
                for (int i = 0; i < argStrings.Length; i++)
                {
                    string argString = argStrings[i];
                    if (!string.IsNullOrWhiteSpace(argString))
                    {
                        ReportInfo($"arg[{i}] = {argString}");
                    }
                }
            }
        }

        private static void OnActivated(object? sender, AppActivationArguments args)
        {
            ExtendedActivationKind kind = args.Kind;
            if (kind == ExtendedActivationKind.Launch)
            {
                ReportLaunchArgs($"OnActivated ({activationCount++})", args);
            }
            else if (kind == ExtendedActivationKind.File)
            {
                ReportFileArgs($"OnActivated ({activationCount++})", args);
            }
        }

        public static void GetActivationInfo()
        {
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            ExtendedActivationKind kind = args.Kind;
            ReportInfo($"ActivationKind: {kind}");

            if (kind == ExtendedActivationKind.Launch)
            {
                if (args.Data is ILaunchActivatedEventArgs launchArgs)
                {
                    string argString = launchArgs.Arguments;
                    string[] argStrings = argString.Split();
                    foreach (string arg in argStrings)
                    {
                        if (!string.IsNullOrWhiteSpace(arg))
                        {
                            ReportInfo(arg);
                        }
                    }
                }
            }
            else if (kind == ExtendedActivationKind.File)
            {
                if (args.Data is IFileActivatedEventArgs fileArgs)
                {
                    IStorageItem? file = fileArgs.Files.ElementAtOrDefault(0);
                    if (file != null)
                    {
                        ReportInfo(file.Name);
                    }
                }
            }
        }

        #endregion


        #region Redirection

        // Decide if we want to redirect the incoming activation to another instance.
        private static bool DecideRedirection()
        {
            try
            {
                bool isRedirect = false;

                // Find out what kind of activation this is.
                AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
                ExtendedActivationKind kind = args.Kind;
                ReportInfo($"ActivationKind={kind}");
                if (kind == ExtendedActivationKind.Launch)
                {
                    // This is a launch activation.
                    ReportLaunchArgs("Main", args);
                }
                else if (kind == ExtendedActivationKind.File)
                {
                    ReportFileArgs("Main", args);

                    try
                    {
                        // This is a file activation: here we'll get the file information,
                        // and register the file name as our instance key.
                        if (args.Data is IFileActivatedEventArgs fileArgs)
                        {
                            IStorageItem file = fileArgs.Files[0];
                            AppInstance keyInstance = AppInstance.FindOrRegisterForKey(file.Name);
                            ReportInfo($"Registered key = {keyInstance.Key}");

                            // If we successfully registered the file name, we must be the
                            // only instance running that was activated for this file.
                            if (keyInstance.IsCurrent)
                            {
                                // Report successful file name key registration.
                                ReportInfo($"IsCurrent=true; registered this instance for {file.Name}");

                                // Hook up the Activated event, to allow for this instance of the app
                                // getting reactivated as a result of multi-instance redirection.
                                keyInstance.Activated += OnActivated;
                            }
                            else
                            {
                                isRedirect = true;
                                RedirectActivationTo(args, keyInstance);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportInfo($"Error getting instance information: {ex.Message}");
                        throw;
                    }
                }

                return isRedirect;
            }
            catch (Exception ex)
            {
                ReportInfo($"Error deciding redirection: {ex.Message}");
                return false;
            }
        }

        // Do the redirection on another thread, and use a non-blocking
        // wait method to wait for the redirection to complete.
        public static void RedirectActivationTo(
            AppActivationArguments args, AppInstance keyInstance)
        {
            var redirectSemaphore = new Semaphore(0, 1);
            Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                redirectSemaphore.Release();
            });
            redirectSemaphore.WaitOne();
        }

        #endregion

    }
}
