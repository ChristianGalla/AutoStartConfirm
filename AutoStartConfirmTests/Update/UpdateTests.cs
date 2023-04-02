using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using FakeItEasy;
using AutoStartConfirm.Notifications;
using Semver;
using AutoStartConfirm.Properties;
using Octokit;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Policy;
using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Xml.Linq;

namespace AutoStartConfirm.Update.Tests
{
    [TestClass()]
    public class UpdateTests
    {
        private readonly ILogger<UpdateService> LogService = A.Fake<ILogger<UpdateService>>();
        private readonly ISettingsService SettingsService = A.Fake<ISettingsService>();
        private readonly INotificationService NotificationService = A.Fake<INotificationService>();
        private readonly IGitHubClient GitHubClient = A.Fake<IGitHubClient>();

        private UpdateService? Service;

        private readonly string CurrentVersion = "1.0.0";
        private readonly string NewestVersion = "2.0.0";
        private readonly string Url = "https://www.example.org";
        private readonly string MsiUrl = "https://www.example.org/AutoStartConfirm.msi";
        private List<ReleaseAsset>? ReleaseAssets;
        private Release? Release;

        [ClassInitialize()]
        public static void ClassInitialize(TestContext context)
        {
        }

        [TestInitialize()]
        public void TestInitialize()
        {
            ReleaseAssets = new List<ReleaseAsset>()
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
            Release = new Release(
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
            A.CallTo(() => SettingsService!.LastNotifiedNewVersion).Returns("");
            A.CallTo(() => GitHubClient.Repository.Release.GetLatest("ChristianGalla", "AutoStartConfirm")).Returns(Release);

            Service = new UpdateService(
                logger: LogService,
                settingsService: SettingsService,
                notificationService: NotificationService,
                gitHubClient: GitHubClient
            );
            Service!.CurrentVersion = new SemVersion(1, 0, 0);
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            Fake.ClearRecordedCalls(LogService);
            Fake.ClearRecordedCalls(SettingsService);
            Fake.ClearRecordedCalls(NotificationService);
            Fake.ClearRecordedCalls(NotificationService);

            Service = null;
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
        }


        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShowsNotificationIfUpdateAavailable()
        {
            await Service!.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService!.ShowNewVersionNotification(NewestVersion, CurrentVersion, MsiUrl)).MustHaveHappenedOnceExactly();
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShouldNotShowNotificationIfNoUpdateAavailable()
        {
            Service!.CurrentVersion = new SemVersion(2, 0, 0);
            await Service!.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService!.ShowNewVersionNotification(A<string>._, A<string>._, A<string>._)).MustNotHaveHappened();
        }

        [TestMethod()]
        public async Task CheckUpdateAndShowNotification_ShouldNotShowNotificationIfAlreadyShown()
        {
            Service!.CurrentVersion = new SemVersion(1, 0, 0);
            A.CallTo(() => SettingsService.LastNotifiedNewVersion).Returns("2.0.0");
            await Service!.CheckUpdateAndShowNotification();

            A.CallTo(() => NotificationService!.ShowNewVersionNotification(A<string>._, A<string>._, A<string>._)).MustNotHaveHappened();
        }
    }
}