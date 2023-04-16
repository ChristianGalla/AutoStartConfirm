// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using AutoStartConfirm.Business;
using AutoStartConfirm.Connectors;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace AutoStartConfirm.GUI
{
    public sealed partial class MainPage : Page, ISubPage, IDisposable
    {
        private bool disposedValue = false;

        public string NavTitile => "Auto Start Confirm";

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

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
                logger ??= ServiceScope.ServiceProvider.GetRequiredService<ILogger<MainPage>>();
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

        private AdvancedCollectionView? autoStartCollectionView;

        public AdvancedCollectionView AutoStartCollectionView
        {
            get
            {
                if (autoStartCollectionView == null)
                {
                    autoStartCollectionView = new AdvancedCollectionView(AutoStartBusiness.CurrentAutoStarts, true);
                    autoStartCollectionView.SortDescriptions.Add(new SortDescription("Date", SortDirection.Descending));
                }
                return autoStartCollectionView;
            }
        }

        private AdvancedCollectionView? historyAutoStartCollectionView;

        public AdvancedCollectionView HistoryAutoStartCollectionView
        {
            get
            {
                if (historyAutoStartCollectionView == null)
                {
                    historyAutoStartCollectionView = new AdvancedCollectionView(AutoStartBusiness.HistoryAutoStarts, true);
                    historyAutoStartCollectionView.SortDescriptions.Add(new SortDescription("Date", SortDirection.Descending));
                }
                return historyAutoStartCollectionView;
            }
        }

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            AutoStartBusiness.CurrentAutoStarts.CollectionChanged += CurrentAutoStarts_CollectionChanged;
            AutoStartCollectionView.PropertyChanged += AutoStartCollectionView_PropertyChanged;
        }

        private void CurrentAutoStarts_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Logger.LogDebug("CurrentAutoStarts_CollectionChanged invoked");
        }

        public void AutoStartCollectionView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Logger.LogDebug("AutoStartCollectionView_PropertyChanged invoked");
        }

        public async void CurrentConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try {
                var button = (Button)sender;
                var autoStartEntry = (AutoStartEntry)button.DataContext;
                await AutoStartBusiness.ConfirmAdd(autoStartEntry);
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to confirm";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }

        public async void CurrentRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStart = (AutoStartEntry)button.DataContext;
            await AutoStartBusiness.RemoveAutoStart(autoStart);
        }

        public async void HistoryConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added)
            {
                await AutoStartBusiness.ConfirmAdd(autoStartEntry);
            }
            else if (autoStartEntry.Change == Change.Removed)
            {
                await AutoStartBusiness.ConfirmRemove(autoStartEntry);
            }
        }

        public async void HistoryRevertButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStart = (AutoStartEntry)button.DataContext;
            switch (autoStart.Change)
            {
                case Change.Added:
                    await AutoStartBusiness.RemoveAutoStart(autoStart);
                    break;
                case Change.Removed:
                    await AutoStartBusiness.AddAutoStart(autoStart);
                    break;
                case Change.Enabled:
                    await AutoStartBusiness.DisableAutoStart(autoStart);
                    break;
                case Change.Disabled:
                    await AutoStartBusiness.EnableAutoStart(autoStart);
                    break;
                default:
                    break;
            }
        }

        public async void Enable_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
            if (toggleSwitch == null || !toggleSwitch.IsEnabled || !toggleSwitch.IsLoaded)
            {
                return;
            }
            var autoStart = (AutoStartEntry)toggleSwitch.DataContext;
            if (!autoStart.IsEnabled.HasValue || toggleSwitch.IsOn == autoStart.IsEnabled.Value)
            {
                return;
            }
            try
            {
                if (toggleSwitch.IsOn && !autoStart.IsEnabled.Value)
                {
                    if (!autoStart.CanBeEnabled.HasValue)
                    {
                        await AutoStartBusiness.LoadCanBeEnabled(autoStart);
                    }
                    if (!autoStart.CanBeEnabled!.Value)
                    {
                        return;
                    }
                    await AutoStartBusiness.EnableAutoStart(autoStart);
                }
                else if (!toggleSwitch.IsOn && autoStart.IsEnabled.Value)
                {
                    if (!autoStart.CanBeDisabled.HasValue)
                    {
                        await AutoStartBusiness.LoadCanBeEnabled(autoStart);
                    }
                    if (!autoStart.CanBeDisabled!.Value)
                    {
                        return;
                    }
                    await AutoStartBusiness.DisableAutoStart(autoStart);
                }
            }
            finally
            {
                // reset toggle
                autoStart.NotifyPropertyChanged("IsEnabled");
            }
        }

        public static void Sorting(object? _, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e, AdvancedCollectionView collectionView, DataGrid dataGrid)
        {
            var newSortColumn = e.Column.Tag.ToString();
            collectionView.SortDescriptions.Clear();
            switch (e.Column.SortDirection)
            {
                case null:
                    collectionView.SortDescriptions.Add(new SortDescription(newSortColumn, SortDirection.Ascending));
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                    break;
                case DataGridSortDirection.Ascending:
                    collectionView.SortDescriptions.Add(new SortDescription(newSortColumn, SortDirection.Descending));
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                    break;
                case DataGridSortDirection.Descending:
                default:
                    e.Column.SortDirection = null;
                    break;
            }

            // Remove sorting indicators from other columns
            foreach (var dgColumn in dataGrid.Columns)
            {
                if (dgColumn.Tag.ToString() != newSortColumn)
                {
                    dgColumn.SortDirection = null;
                }
            }
        }

        public void CurrentSorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
        {
            Sorting(sender, e, AutoStartCollectionView, CurrentAutoStartGrid);
        }

        public void HistorySorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
        {
            Sorting(sender, e, HistoryAutoStartCollectionView, HistoryAutoStartGrid);
        }

        public static bool CanBeConfirmedConverter(AutoStartEntry autoStart)
        {
            var status = autoStart.ConfirmStatus;
            return status == ConfirmStatus.New;
        }

        public static bool CanBeAddedConverter(AutoStartEntry autoStart)
        {
            if (autoStart.CanBeAdded.HasValue)
            {
                return autoStart.CanBeAdded.Value;
            }
            if (autoStart.CanBeAddedLoader == null)
            {
                using var ServiceScope = Ioc.Default.CreateScope();
                var AutoStartBusiness = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
                AutoStartBusiness.LoadCanBeAdded(autoStart);
            }
            return false;
        }

        public static bool CanBeDisabledConverter(AutoStartEntry autoStart)
        {
            if (autoStart.CanBeDisabled.HasValue)
            {
                return autoStart.CanBeDisabled.Value;
            }
            if (autoStart.CanBeDisabledLoader == null)
            {
                using var ServiceScope = Ioc.Default.CreateScope();
                var AutoStartBusiness = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
                AutoStartBusiness.LoadCanBeDisabled(autoStart);
            }
            return false;
        }

        public static bool CanBeEnabledConverter(AutoStartEntry autoStart)
        {
            if (autoStart.CanBeEnabled.HasValue)
            {
                return autoStart.CanBeEnabled.Value;
            }
            if (autoStart.CanBeEnabledLoader == null)
            {
                using var ServiceScope = Ioc.Default.CreateScope();
                var AutoStartBusiness = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
                AutoStartBusiness.LoadCanBeEnabled(autoStart);
            }
            return false;
        }

        public static bool CanBeRemovedConverter(AutoStartEntry autoStart)
        {
            if (autoStart.CanBeRemoved.HasValue)
            {
                return autoStart.CanBeRemoved.Value;
            }
            if (autoStart.CanBeRemovedLoader == null)
            {
                using var ServiceScope = Ioc.Default.CreateScope();
                var AutoStartBusiness = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
                AutoStartBusiness.LoadCanBeRemoved(autoStart);
            }
            return false;
        }

        public static bool CanBeRevertedConverter(AutoStartEntry autoStart)
        {
            using var ServiceScope = Ioc.Default.CreateScope();
            var AutoStartBusiness = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
            switch (autoStart.Change)
            {
                case Change.Added:
                    if (autoStart.CanBeRemoved.HasValue)
                    {
                        return autoStart.CanBeRemoved.Value;
                    }
                    if (autoStart.CanBeRemovedLoader == null)
                    {
                        AutoStartBusiness.LoadCanBeRemoved(autoStart);
                    }
                    break;
                case Change.Removed:
                    if (autoStart.CanBeAdded.HasValue)
                    {
                        return autoStart.CanBeAdded.Value;
                    }
                    if (autoStart.CanBeAddedLoader == null)
                    {
                        AutoStartBusiness.LoadCanBeAdded(autoStart);
                    }
                    break;
                case Change.Enabled:
                    if (autoStart.CanBeDisabled.HasValue)
                    {
                        return autoStart.CanBeDisabled.Value;
                    }
                    if (autoStart.CanBeDisabledLoader == null)
                    {
                        AutoStartBusiness.LoadCanBeDisabled(autoStart);
                    }
                    break;
                case Change.Disabled:
                    if (autoStart.CanBeEnabled.HasValue)
                    {
                        return autoStart.CanBeEnabled.Value;
                    }
                    if (autoStart.CanBeEnabledLoader == null)
                    {
                        AutoStartBusiness.LoadCanBeEnabled(autoStart);
                    }
                    break;
            }
            return false;
        }

        public static bool CanBeToggledConverter(AutoStartEntry autoStart)
        {
            if (!autoStart.IsEnabled.HasValue)
            {
                return false;
            }
            using var ServiceScope = Ioc.Default.CreateScope();
            var AutoStartBusiness = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
            if (autoStart.IsEnabled.Value)
            {
                if (!autoStart.CanBeDisabled.HasValue)
                {
                    AutoStartBusiness.LoadCanBeDisabled(autoStart);
                }
                else
                {
                    return autoStart.CanBeDisabled.Value;
                }
            }
            else
            {
                if (!autoStart.CanBeEnabled.HasValue)
                {
                    AutoStartBusiness.LoadCanBeEnabled(autoStart);
                }
                else
                {
                    return autoStart.CanBeEnabled.Value;
                }
            }
            return false;
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
