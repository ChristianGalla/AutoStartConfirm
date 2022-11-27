using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using FakeItEasy;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Update;

namespace AutoStartConfirm.Tests
{
    [TestClass()]
    public class AppTests {
        private IAutoStartService AutoStartService = A.Fake<IAutoStartService>();
        private INotificationService NotificationService = A.Fake<INotificationService>();
        private IMessageService MessageService = A.Fake<IMessageService>();
        private ISettingsService SettingsService = A.Fake<ISettingsService>();
        private IUpdateService UpdateService = A.Fake<IUpdateService>();

        private static App app;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context) {
            app = new App();
        }

        [TestInitialize]
        public void TestInitialize() {
            AddFakes(app);
        }

        private void AddFakes(App app)
        {
            Fake.ClearRecordedCalls(AutoStartService);
            Fake.ClearRecordedCalls(NotificationService);
            Fake.ClearRecordedCalls(MessageService);
            Fake.ClearRecordedCalls(SettingsService);
            Fake.ClearRecordedCalls(UpdateService);

            app.AutoStartService = AutoStartService;
            app.NotificationService = NotificationService;
            app.MessageService = MessageService;
            app.SettingsService = SettingsService;
            app.UpdateService = UpdateService;
        }

        [TestCleanup]
        public void TestCleanup() {
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup() {
            if (app != null) {
                app.Dispose();
                app = null;
            }
        }

        [TestMethod()]
        public void Start_LoadsAutoStarts_And_StartsWatchers() {

            app.Start(true);

            A.CallTo(() => AutoStartService.LoadCurrentAutoStarts()).MustHaveHappened();
            A.CallTo(() => AutoStartService.StartWatcher()).MustHaveHappened();
        }

        [TestMethod()]
        public void Start_ChecksForUpdatesIfEnabled()
        {
            A.CallTo(() => SettingsService.CheckForUpdatesOnStart).Returns(true);

            app.Start(true);

            A.CallTo(() => UpdateService.CheckUpdateAndShowNotification()).MustHaveHappened();
        }

        [TestMethod()]
        public void Start_ChecksNotForUpdatesIfDisabled()
        {
            A.CallTo(() => SettingsService.CheckForUpdatesOnStart).Returns(false);

            app.Start(true);

            A.CallTo(() => UpdateService.CheckUpdateAndShowNotification()).MustNotHaveHappened();
        }

        [TestMethod()]
        public void ToggleOwnAutoStart_ShowsErrorMessageOnError() {
            A.CallTo(() => AutoStartService.ToggleOwnAutoStart()).Throws(new Exception());
            app.ToggleOwnAutoStart().Wait();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
            Fake.ClearRecordedCalls(MessageService);
        }

        [TestMethod()]
        public void ToggleOwnAutoStart_ShowsNoErrorMessageIfNoError() {
            app.ToggleOwnAutoStart().Wait();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            Fake.ClearRecordedCalls(MessageService);
        }
    }
}