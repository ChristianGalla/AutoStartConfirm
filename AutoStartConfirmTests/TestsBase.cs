using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Properties;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using AutoStartConfirm.Notifications;
using Octokit;

namespace AutoStartConfirmTests
{
    public class TestsBase
    {
        protected static readonly IAutoStartConnectorService ConnectorService = A.Fake<IAutoStartConnectorService>();
        protected static readonly ISettingsService SettingsService = A.Fake<ISettingsService>();
        protected static readonly ICurrentUserRun64Connector CurrentUserRun64Connector = A.Fake<ICurrentUserRun64Connector>();
        protected static readonly IDispatchService DispatchService = A.Fake<IDispatchService>();
        protected static readonly IUacService UacService = A.Fake<IUacService>();
        protected static readonly IServiceProvider ServiceProvider = A.Fake<IServiceProvider>();
        protected static readonly INotificationService NotificationService = A.Fake<INotificationService>();
        protected static readonly IGitHubClient GitHubClient = A.Fake<IGitHubClient>();


        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            Ioc.Default.ConfigureServices(ServiceProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Fake.ClearRecordedCalls(ConnectorService);
            Fake.ClearRecordedCalls(SettingsService);
            Fake.ClearRecordedCalls(CurrentUserRun64Connector);
            Fake.ClearRecordedCalls(DispatchService);
            Fake.ClearRecordedCalls(UacService);
            Fake.ClearRecordedCalls(ServiceProvider);
            Fake.ClearRecordedCalls(NotificationService);
            Fake.ClearRecordedCalls(GitHubClient);
        }
    }
}