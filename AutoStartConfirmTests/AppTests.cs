using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using FakeItEasy;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Update;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.ServiceProcess;

namespace AutoStartConfirm.Tests
{
    [TestClass()]
    public class AppTests
    {
        private readonly ILogger<App> LogService = A.Fake<ILogger<App>>();
        private readonly IAppStatus AppStatus = A.Fake<IAppStatus>();
        private readonly IAutoStartService AutoStartService = A.Fake<IAutoStartService>();
        private readonly INotificationService NotificationService = A.Fake<INotificationService>();
        private readonly IMessageService MessageService = A.Fake<IMessageService>();
        private readonly ISettingsService SettingsService = A.Fake<ISettingsService>();
        private readonly IUpdateService UpdateService = A.Fake<IUpdateService>();
        private readonly IDispatchService DispatchService = A.Fake<IDispatchService>();

        private App? app;

        [ClassInitialize()]
        public static void ClassInitialize(TestContext context) {
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            ServiceCollection services = new();
            services.AddSingleton<NotifyIcon>(sp => A.Fake<NotifyIcon>());
            services.AddSingleton<MainWindow>(sp => A.Fake<MainWindow>());
            var ServiceProvider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(ServiceProvider);
            app = new App(
                logger: LogService,
                appStatus: AppStatus,
                autoStartService: AutoStartService,
                notificationService: NotificationService,
                messageService: MessageService,
                settingsService: SettingsService,
                updateService: UpdateService,
                dispatchService: DispatchService
            );
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            Fake.ClearRecordedCalls(LogService);
            Fake.ClearRecordedCalls(AppStatus);
            Fake.ClearRecordedCalls(AutoStartService);
            Fake.ClearRecordedCalls(NotificationService);
            Fake.ClearRecordedCalls(MessageService);
            Fake.ClearRecordedCalls(SettingsService);
            Fake.ClearRecordedCalls(UpdateService);
            Fake.ClearRecordedCalls(DispatchService);

            app?.Dispose();
            app = null;
        }

        [ClassCleanup()]
        public static void ClassCleanup() {
        }

        [TestMethod()]
        public void Start_LoadsAutoStarts_And_StartsWatchers() {
            app!.Run();
            A.CallTo(() => AutoStartService.LoadCurrentAutoStarts()).MustHaveHappened();
            A.CallTo(() => AutoStartService.StartWatcher()).MustHaveHappened();
        }

        [TestMethod()]
        public void Start_ChecksForUpdatesIfEnabled()
        {
            A.CallTo(() => SettingsService.CheckForUpdatesOnStart).Returns(true);
            app!.Run();
            A.CallTo(() => UpdateService.CheckUpdateAndShowNotification()).MustHaveHappened();
        }

        [TestMethod()]
        public void Start_ChecksNotForUpdatesIfDisabled()
        {
            A.CallTo(() => SettingsService.CheckForUpdatesOnStart).Returns(false);
            app!.Run();
            A.CallTo(() => UpdateService.CheckUpdateAndShowNotification()).MustNotHaveHappened();
        }

        [TestMethod()]
        public void ToggleOwnAutoStart_ShowsErrorMessageOnError() {
            A.CallTo(() => AutoStartService.ToggleOwnAutoStart()).Throws(new Exception());
            app!.ToggleOwnAutoStart().Wait();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
            Fake.ClearRecordedCalls(MessageService);
        }

        [TestMethod()]
        public void ToggleOwnAutoStart_ShowsNoErrorMessageIfNoError() {
            app!.ToggleOwnAutoStart().Wait();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            Fake.ClearRecordedCalls(MessageService);
        }
    }
}