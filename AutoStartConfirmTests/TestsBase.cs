﻿using AutoStartConfirm.Connectors.Registry;
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
using AutoStartConfirm.Models;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Update;

namespace AutoStartConfirm
{
    public class TestsBase
    {
        protected static readonly IServiceProvider ServiceProvider = A.Fake<IServiceProvider>();
        protected IAutoStartConnectorService ConnectorService = A.Fake<IAutoStartConnectorService>();
        protected ISettingsService SettingsService = A.Fake<ISettingsService>();
        protected ICurrentUserRun64Connector CurrentUserRun64Connector = A.Fake<ICurrentUserRun64Connector>();
        protected IDispatchService DispatchService = A.Fake<IDispatchService>();
        
        protected IUacService UacService = A.Fake<IUacService>();
        protected INotificationService NotificationService = A.Fake<INotificationService>();
        protected IGitHubClient GitHubClient = A.Fake<IGitHubClient>();
        protected IAppStatus AppStatus = A.Fake<IAppStatus>();
        protected IMessageService MessageService = A.Fake<IMessageService>();
        protected IUpdateService UpdateService = A.Fake<IUpdateService>();


        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            Ioc.Default.ConfigureServices(ServiceProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Fake.ClearRecordedCalls(ServiceProvider);

            ConnectorService = A.Fake<IAutoStartConnectorService>();
            SettingsService = A.Fake<ISettingsService>();
            CurrentUserRun64Connector = A.Fake<ICurrentUserRun64Connector>();
            DispatchService = A.Fake<IDispatchService>();
            UacService = A.Fake<IUacService>();
            NotificationService = A.Fake<INotificationService>();
            GitHubClient = A.Fake<IGitHubClient>();
            AppStatus = A.Fake<IAppStatus>();
            MessageService = A.Fake<IMessageService>();
            UpdateService = A.Fake<IUpdateService>();
        }
    }
}