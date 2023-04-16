// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using AutoStartConfirm.Business;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace AutoStartConfirm.GUI
{
    public sealed partial class SettingsPage : Page, ISubPage, IDisposable
    {
        private bool disposedValue = false;

        public string NavTitile => "Settings";

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


        public SettingsPage()
        {
            Connectors = new SortedList<string, ConnectorEnableRow>();
            foreach (Category category in Enum.GetValues(typeof(Category)))
            {
                // todo: Change to better readable title and add description
                var row = new ConnectorEnableRow()
                {
                    Category = category,
                };
                Connectors.Add(row.CategoryName, row);
            }

            InitializeComponent();
            EnabledConnectorList.Loaded += EnabledConnectorList_Loaded;
            NavigationCacheMode = NavigationCacheMode.Enabled;

        }

        // Setting selected items only works after list view has been rendered
        private void EnabledConnectorList_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var row in Connectors.Values)
            {
                if (!SettingsService.DisabledConnectors.Contains(row.CategoryName))
                {
                    EnabledConnectorList.SelectedItems.Add(row);
                }
            }
        }

        private void ConnectorSelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            foreach (ConnectorEnableRow enabled in args.AddedItems)
            {
                while (SettingsService.DisabledConnectors.Contains(enabled.CategoryName))
                {
                    SettingsService.DisabledConnectors.Remove(enabled.CategoryName);
                }
            }

            foreach (ConnectorEnableRow disabled in args.RemovedItems)
            {
                SettingsService.DisabledConnectors.Add(disabled.CategoryName);
            }
        }

        private async void OwnAutoStart_Toggled(object sender, RoutedEventArgs e)
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
