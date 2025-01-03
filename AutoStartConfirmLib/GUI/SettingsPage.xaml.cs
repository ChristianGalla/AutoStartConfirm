// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using AutoStartConfirm.Business;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;

namespace AutoStartConfirm.GUI
{
    public sealed partial class SettingsPage : Page, ISubPage, IDisposable
    {
        private bool disposedValue = false;

        public string NavTitle { get; set; }

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        public SortedList<string, ConnectorEnableRow> Connectors;

#pragma warning disable CA2213 // Disposable fields should be disposed
        // Disposed by ServiceProvider
        private ISettingsService? settingsService;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public ISettingsService SettingsService {
            get {
                settingsService ??= ServiceScope.ServiceProvider.GetRequiredService<ISettingsService>();
                return settingsService;
            }
        }

        private IAutoStartBusiness? autoStartBusiness;

        public IAutoStartBusiness AutoStartBusiness
        {
            get
            {
                autoStartBusiness ??= ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
                return autoStartBusiness;
            }
        }

        private ILogger? logger;

        public ILogger Logger
        {
            get
            {
                logger ??= ServiceScope.ServiceProvider.GetRequiredService<ILogger<SettingsPage>>();
                return logger;
            }
        }

        private IAppStatus? appStatus;

        public IAppStatus AppStatus
        {
            get
            {
                appStatus ??= ServiceScope.ServiceProvider.GetRequiredService<IAppStatus>();
                return appStatus;
            }
        }

        private IMessageService? messageService;

        public IMessageService MessageService
        {
            get
            {
                messageService ??= ServiceScope.ServiceProvider.GetRequiredService<IMessageService>();
                return messageService;
            }
        }


        public SettingsPage()
        {
            Connectors = new SortedList<string, ConnectorEnableRow>();
            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                // todo: Change to better readable title and add description
                var row = new ConnectorEnableRow()
                {
                    Category = category,
                    Enabled = !SettingsService.DisabledConnectors.Contains(category.ToString())
                };
                Connectors.Add(row.CategoryName, row);
            }

            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            var resourceLoader = new ResourceLoader("AutoStartConfirmLib/Resources");
            NavTitle = resourceLoader.GetString("NavigationSettings/Content");
        }

        public void ConnectorEnable_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
            if (toggleSwitch == null || !toggleSwitch.IsEnabled || !toggleSwitch.IsLoaded)
            {
                return;
            }
            var connectorRow = (ConnectorEnableRow)toggleSwitch.DataContext;
            if (connectorRow == null)
            {
                return;
            }
            connectorRow.Enabled = toggleSwitch.IsOn;
            if (toggleSwitch.IsOn)
            {
                SettingsService.DisabledConnectors.Remove(connectorRow.CategoryName);
            }
            else
            {
                SettingsService.DisabledConnectors.Add(connectorRow.CategoryName);
            }
            SettingsService.Save();
        }


#pragma warning disable IDE0060 // Remove unused parameter
        private async void OwnAutoStart_Toggled(object sender, RoutedEventArgs e)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
            if (toggleSwitch == null || !toggleSwitch.IsEnabled || !toggleSwitch.IsLoaded)
            {
                return;
            }
            if (AppStatus.HasOwnAutoStart == toggleSwitch.IsOn)
            {
                return;
            }
            await AutoStartBusiness.ToggleOwnAutoStart();
        }


#pragma warning disable IDE0060 // Remove unused parameter
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            AutoStartBusiness.ClearHistory();
        }


        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceScope.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
