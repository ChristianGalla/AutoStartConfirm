using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using FakeItEasy;
using AutoStartConfirm.Notifications;
using Semver;
using AutoStartConfirm.Properties;

namespace AutoStartConfirm.Update.Tests
{
    [TestClass()]
    public class UpdateTests
    {

        private UpdateService Service = new UpdateService();
        INotificationService NotificationService = A.Fake<INotificationService>();
        ISettingsService SettingsService = A.Fake<ISettingsService>();

        [TestInitialize]
        public void TestInitialize()
        {
            A.CallTo(() => SettingsService.LastNotifiedNewVersion).Returns("");
            Fake.ClearRecordedCalls(NotificationService);
            Service.NotificationService = NotificationService;
            Service.SettingsService = SettingsService;
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShowsNotificationIfUpdateAavailable()
        {
            string currentVersion = "1.0.0";
            string newestVersion = "2.0.0";
            Service.CurrentVersion = new SemVersion(1, 0, 0);
            Service.NewestVersion = Task.Run(() => new SemVersion(2, 0, 0));
            await Service.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService.ShowNewVersionNotification(newestVersion, currentVersion)).MustHaveHappenedOnceExactly();
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShouldNotShowNotificationIfNoUpdateAavailable()
        {
            Service.CurrentVersion = new SemVersion(1, 0, 0);
            Service.NewestVersion = Task.Run(() => new SemVersion(1, 0, 0));
            await Service.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService.ShowNewVersionNotification(A<string>._, A<string>._)).MustNotHaveHappened();
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShouldNotShowNotificationIfAlreadyShown()
        {
            Service.CurrentVersion = new SemVersion(1, 0, 0);
            Service.NewestVersion = Task.Run(() => new SemVersion(2, 0, 0));
            A.CallTo(() => SettingsService.LastNotifiedNewVersion).Returns("2.0.0");
            await Service.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService.ShowNewVersionNotification(A<string>._, A<string>._)).MustNotHaveHappened();
        }
    }
}