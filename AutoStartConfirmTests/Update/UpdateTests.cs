using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using FakeItEasy;
using AutoStartConfirm.Notifications;
using Semver;
using AutoStartConfirm.Properties;
using Octokit;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Policy;

namespace AutoStartConfirm.Update.Tests
{
    [TestClass()]
    public class UpdateTests
    {

        UpdateService Service = new UpdateService();
        static INotificationService NotificationService = A.Fake<INotificationService>();
        static ISettingsService SettingsService = A.Fake<ISettingsService>();
        static IGitHubClient GitHubClient = A.Fake<IGitHubClient>();
        static string CurrentVersion = "1.0.0";
        static string NewestVersion = "2.0.0";
        static string Url = "https://www.example.org";
        static string MsiUrl = "https://www.example.org/AutoStartConfirm.msi";
        static List<ReleaseAsset> ReleaseAssets = new List<ReleaseAsset>()
        {
            new ReleaseAsset(
                url: MsiUrl,
                id: 1,
                nodeId: "",
                name: "AutoStartConfirm.msi",
                label: "",
                state: "",
                contentType: "",
                size: 1,
                downloadCount: 1,
                createdAt: new DateTimeOffset(),
                updatedAt: new DateTimeOffset(),
                browserDownloadUrl: MsiUrl,
                uploader: null)
        };
        static Release Release = new Release(
            url: Url,
            htmlUrl: Url,
            assetsUrl: Url,
            uploadUrl: Url,
            id: 1,
            nodeId: "",
            tagName: NewestVersion,
            targetCommitish: "",
            name: NewestVersion,
            body: "",
            draft: false,
            prerelease: false,
            createdAt: new DateTimeOffset(),
            publishedAt: new DateTimeOffset(),
            author: null,
            tarballUrl: Url,
            zipballUrl: Url,
            assets: ReleaseAssets
        );

        [TestInitialize]
        public void TestInitialize()
        {
            A.CallTo(() => SettingsService.LastNotifiedNewVersion).Returns("");
            Fake.ClearRecordedCalls(NotificationService);
            Service.NotificationService = NotificationService;
            Service.SettingsService = SettingsService;
            Service.GitHubClient = GitHubClient;
            Service.CurrentVersion = new SemVersion(1, 0, 0);
            
            A.CallTo(() => GitHubClient.Repository.Release.GetLatest("ChristianGalla", "AutoStartConfirm")).Returns(Release);
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShowsNotificationIfUpdateAavailable()
        {
            await Service.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService.ShowNewVersionNotification(NewestVersion, CurrentVersion, MsiUrl)).MustHaveHappenedOnceExactly();
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShouldNotShowNotificationIfNoUpdateAavailable()
        {
            Service.CurrentVersion = new SemVersion(2, 0, 0);
            await Service.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService.ShowNewVersionNotification(A<string>._, A<string>._, A<string>._)).MustNotHaveHappened();
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShouldNotShowNotificationIfAlreadyShown()
        {
            Service.CurrentVersion = new SemVersion(1, 0, 0);
            A.CallTo(() => SettingsService.LastNotifiedNewVersion).Returns("2.0.0");
            await Service.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService.ShowNewVersionNotification(A<string>._, A<string>._, A<string>._)).MustNotHaveHappened();
        }
    }
}