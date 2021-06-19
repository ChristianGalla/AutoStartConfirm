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

namespace AutoStartConfirm.Tests {
    [TestClass()]
    public class AppTests {
        private IAutoStartService AutoStartService = A.Fake<IAutoStartService>();
        private INotificationService NotificationServicee = A.Fake<INotificationService>();
        private IMessageService MessageService = A.Fake<IMessageService>();
        private static string currentExePath = "C:\\test.exe";

        private AutoStartEntry ownAutoStartEntry = new RegistryAutoStartEntry() {
            Category = Category.CurrentUserRun64,
            Path = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm",
            Value = currentExePath
        };

        static private App app;

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
            app.CurrentExePath = currentExePath;
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
        public void ToggleOwnAutoStart_AddsOwnAutoStart_If_NotSet() {
            app.ToggleOwnAutoStart().Wait();
            A.CallTo(() => AutoStartService.AddAutoStart(A<AutoStartEntry>.Ignored)).WhenArgumentsMatch(
                (AutoStartEntry autoStart) =>
                    autoStart.Category == Category.CurrentUserRun64 &&
                    autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
                    autoStart.Value == currentExePath
            ).MustHaveHappened();
        }

        [TestMethod()]
        public void ToggleOwnAutoStart_RemovesOwnAutoStart_If_Set() {
            var collection = new ObservableCollection<AutoStartEntry>() {
                ownAutoStartEntry
            };
            A.CallTo(() => AutoStartService.CurrentAutoStarts).Returns(collection);
            app.ToggleOwnAutoStart().Wait();
            A.CallTo(() => AutoStartService.RemoveAutoStart(A<AutoStartEntry>.Ignored)).WhenArgumentsMatch(
                (AutoStartEntry autoStart) =>
                    autoStart.Category == Category.CurrentUserRun64 &&
                    autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
                    autoStart.Value == currentExePath
            ).MustHaveHappened();
        }

        [TestMethod()]
        public void ToggleOwnAutoStart_ShowsErrorMessageOnError() {
            A.CallTo(() => AutoStartService.AddAutoStart(A<AutoStartEntry>.Ignored)).Throws(new Exception());
            app.ToggleOwnAutoStart().Wait();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
            Fake.ClearRecordedCalls(MessageService);
        }
    }
}