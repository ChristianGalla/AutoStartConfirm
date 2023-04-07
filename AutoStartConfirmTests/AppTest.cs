//using AutoStartConfirm;
//using AutoStartConfirm.Connectors;
//using AutoStartConfirm.GUI;
//using AutoStartConfirm.Helpers;
//using AutoStartConfirm.Models;
//using AutoStartConfirm.Notifications;
//using AutoStartConfirm.Properties;
//using AutoStartConfirm.Update;
//using CommunityToolkit.Mvvm.DependencyInjection;
//using FakeItEasy;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Update;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirmTests
{
    [TestClass]
    public class AppTest
    {
        //private readonly ILogger<AutoStartConfirm.App> LogService = A.Fake<ILogger<AutoStartConfirm.App>>();
        //private readonly IAppStatus AppStatus = A.Fake<IAppStatus>();
        //private readonly IAutoStartService AutoStartService = A.Fake<IAutoStartService>();
        //private readonly INotificationService NotificationService = A.Fake<INotificationService>();
        //private readonly IMessageService MessageService = A.Fake<IMessageService>();
        //private readonly ISettingsService SettingsService = A.Fake<ISettingsService>();
        //private readonly IUpdateService UpdateService = A.Fake<IUpdateService>();
        //private readonly IDispatchService DispatchService = A.Fake<IDispatchService>();

        // private AutoStartConfirm.App? app;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
            //Fake.ClearRecordedCalls(LogService);
            //Fake.ClearRecordedCalls(AppStatus);
            //Fake.ClearRecordedCalls(AutoStartService);
            //Fake.ClearRecordedCalls(NotificationService);
            //Fake.ClearRecordedCalls(MessageService);
            //Fake.ClearRecordedCalls(SettingsService);
            //Fake.ClearRecordedCalls(UpdateService);
            //Fake.ClearRecordedCalls(DispatchService);

            //app?.Dispose();
            //app = null;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        //[UITestMethod()]
        //public void CanBeCreated()
        //{
        //    //ServiceCollection services = new();
        //    //services.AddSingleton<NotifyIcon>(sp => A.Fake<NotifyIcon>());
        //    //services.AddSingleton<MainWindow>(sp => A.Fake<MainWindow>());
        //    //var ServiceProvider = services.BuildServiceProvider();
        //    //Ioc.Default.ConfigureServices(ServiceProvider);
        //    //app = new AutoStartConfirm.App(
        //    //    logger: LogService,
        //    //    appStatus: AppStatus,
        //    //    autoStartService: AutoStartService,
        //    //    notificationService: NotificationService,
        //    //    messageService: MessageService,
        //    //    settingsService: SettingsService,
        //    //    updateService: UpdateService,
        //    //    dispatchService: DispatchService
        //    //);

        //    //app!.Run();
        //}

        //[TestMethod]
        //public void Start_LoadsAutoStarts_And_StartsWatchers()
        //{
        //    WinRT.ComWrappersSupport.InitializeComWrappers();
        //    Microsoft.UI.Xaml.Application.Start((p) =>
        //    {
        //        ServiceCollection services = new();
        //        services.AddSingleton<NotifyIcon>(sp => A.Fake<NotifyIcon>());
        //        services.AddSingleton<MainWindow>(sp => A.Fake<MainWindow>());
        //        var ServiceProvider = services.BuildServiceProvider();
        //        Ioc.Default.ConfigureServices(ServiceProvider);
        //        app = new App(
        //            logger: LogService,
        //            appStatus: AppStatus,
        //            autoStartService: AutoStartService,
        //            notificationService: NotificationService,
        //            messageService: MessageService,
        //            settingsService: SettingsService,
        //            updateService: UpdateService,
        //            dispatchService: DispatchService
        //        );

        //        app!.Run();
        //    });

        //    A.CallTo(() => AutoStartService.LoadCurrentAutoStarts()).MustHaveHappened();
        //    A.CallTo(() => AutoStartService.StartWatcher()).MustHaveHappened();
        //}

        //[TestMethod]
        //public void Start_ChecksForUpdatesIfEnabled()
        //{
        //    A.CallTo(() => SettingsService.CheckForUpdatesOnStart).Returns(true);
        //    app!.Run();
        //    A.CallTo(() => UpdateService.CheckUpdateAndShowNotification()).MustHaveHappened();
        //}

        //[TestMethod]
        //public void Start_ChecksNotForUpdatesIfDisabled()
        //{
        //    A.CallTo(() => SettingsService.CheckForUpdatesOnStart).Returns(false);
        //    app!.Run();
        //    A.CallTo(() => UpdateService.CheckUpdateAndShowNotification()).MustNotHaveHappened();
        //}

        //[TestMethod]
        //public void ToggleOwnAutoStart_ShowsErrorMessageOnError()
        //{
        //    A.CallTo(() => AutoStartService.ToggleOwnAutoStart()).Throws(new Exception());
        //    app!.ToggleOwnAutoStart().Wait();
        //    A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
        //    Fake.ClearRecordedCalls(MessageService);
        //}

        //[TestMethod]
        //public void ToggleOwnAutoStart_ShowsNoErrorMessageIfNoError()
        //{
        //    app!.ToggleOwnAutoStart().Wait();
        //    A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
        //    Fake.ClearRecordedCalls(MessageService);
        //}
    }
}
