using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoStartConfirm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using AutoStartConfirm.GUI;
using System.Collections.ObjectModel;
using AutoStartConfirm.Properties;

namespace AutoStartConfirm.Tests {
    [TestClass()]
    public class AppTests {
        private IAutoStartService AutoStartService = A.Fake<IAutoStartService>();
        private INotificationService NotificationServicee = A.Fake<INotificationService>();
        private IMessageService MessageService = A.Fake<IMessageService>();
        private ISettingsService SettingsService = A.Fake<ISettingsService>();

        private static App app;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context) {
            app = new App();
        }

        [TestInitialize]
        public void TestInitialize() {
            AddFakes(app);
        }

        private void AddFakes(App app) {
            app.AutoStartService = AutoStartService;
            app.NotificationService = NotificationServicee;
            app.MessageService = MessageService;
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