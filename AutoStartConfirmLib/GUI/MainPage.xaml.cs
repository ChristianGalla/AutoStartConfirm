// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AutoStartConfirm.GUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, ISubPage, IDisposable
    {
        private bool disposedValue = false;

        public string NavTitile => "Auto Start Confirm";

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        private IAutoStartService? autoStartService;

        public IAutoStartService AutoStartService
        {
            get
            {
                autoStartService ??= ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
                return autoStartService;
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
                    autoStartCollectionView = new AdvancedCollectionView(AutoStartService.CurrentAutoStarts, true);
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
                    historyAutoStartCollectionView = new AdvancedCollectionView(AutoStartService.HistoryAutoStarts, true);
                    historyAutoStartCollectionView.SortDescriptions.Add(new SortDescription("Date", SortDirection.Descending));
                }
                return historyAutoStartCollectionView;
            }
        }

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            AutoStartService.CurrentAutoStarts.CollectionChanged += CurrentAutoStarts_CollectionChanged; ;
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

        private async void CurrentConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try {
                var button = (Button)sender;
                var autoStartEntry = (AutoStartEntry)button.DataContext;
                AutoStartService.ConfirmAdd(autoStartEntry);
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to confirm";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }

        private async void CurrentRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            try {
                var button = (Button)sender;
                var autoStart = (AutoStartEntry)button.DataContext;
                if (!await MessageService.ShowConfirm(autoStart, "remove"))
                {
                    return;
                }
                AutoStartService.RemoveAutoStart(autoStart);
                await MessageService.ShowSuccess("Removed auto start");
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to remove auto start";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }

        private async void HistoryConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var autoStartEntry = (AutoStartEntry)button.DataContext;
                if (autoStartEntry.Change == Change.Added)
                {
                    AutoStartService.ConfirmAdd(autoStartEntry);
                }
                else if (autoStartEntry.Change == Change.Removed)
                {
                    AutoStartService.ConfirmRemove(autoStartEntry);
                }
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to confirm";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }

        private async void HistoryRevertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var autoStart = (AutoStartEntry)button.DataContext;
                switch (autoStart.Change)
                {
                    case Change.Added:
                        if (!await MessageService.ShowConfirm(autoStart, "remove"))
                        {
                            return;
                        }
                        AutoStartService.RemoveAutoStart(autoStart);
                        await MessageService.ShowSuccess("Removed auto start");
                        break;
                    case Change.Removed:
                        if (!await MessageService.ShowConfirm(autoStart, "add"))
                        {
                            return;
                        }
                        AutoStartService.AddAutoStart(autoStart);
                        await MessageService.ShowSuccess("Added auto start");
                        break;
                    case Change.Enabled:
                        if (!await MessageService.ShowConfirm(autoStart, "disable"))
                        {
                            return;
                        }
                        AutoStartService.DisableAutoStart(autoStart);
                        await MessageService.ShowSuccess("Disabled auto start");
                        break;
                    case Change.Disabled:
                        if (!await MessageService.ShowConfirm(autoStart, "enable"))
                        {
                            return;
                        }
                        AutoStartService.EnableAutoStart(autoStart);
                        await MessageService.ShowSuccess("Enabled auto start");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to revert";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }

        private async void Enable_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
                if (toggleSwitch == null || !toggleSwitch.IsEnabled || !toggleSwitch.IsLoaded)
                {
                    return;
                }
                var autoStart = (AutoStartEntry)toggleSwitch.DataContext;
                try
                {
                    if (!autoStart.IsEnabled.HasValue || toggleSwitch.IsOn == autoStart.IsEnabled.Value)
                    {
                        return;
                    }
                    if (toggleSwitch.IsOn && !autoStart.IsEnabled.Value)
                    {
                        if (!autoStart.CanBeEnabled.HasValue)
                        {
                            await AutoStartService.LoadCanBeDisabled(autoStart);
                        }
#pragma warning disable CS8629 // Nullable value type may be null.
                        if (!autoStart.CanBeEnabled.Value)
                        {
                            return;
                        }
#pragma warning restore CS8629 // Nullable value type may be null.
                        if (!await MessageService.ShowConfirm(autoStart, "enable"))
                        {
                            // reset toggle
                            autoStart.NotifyPropertyChanged("IsEnabled");
                            return;
                        }
                        AutoStartService.EnableAutoStart(autoStart);
                        await MessageService.ShowSuccess("Enabled auto start");
                    }
                    else if (!toggleSwitch.IsOn && autoStart.IsEnabled.Value)
                    {
                        if (!autoStart.CanBeDisabled.HasValue)
                        {
                            await AutoStartService.LoadCanBeEnabled(autoStart);
                        }
#pragma warning disable CS8629 // Nullable value type may be null.
                        if (!autoStart.CanBeDisabled.Value)
                        {
                            return;
                        }
#pragma warning restore CS8629 // Nullable value type may be null.
                        if (!await MessageService.ShowConfirm(autoStart, "disable"))
                        {
                            // reset toggle
                            autoStart.NotifyPropertyChanged("IsEnabled");
                            return;
                        }
                        AutoStartService.DisableAutoStart(autoStart);
                        await MessageService.ShowSuccess("Disabled auto start");
                    }
                }
                catch (Exception)
                {
                    // reset toggle
                    autoStart.NotifyPropertyChanged("IsEnabled");
                    throw;
                }
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to toggle enable status of auto start";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }

        private static void Sorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e, AdvancedCollectionView collectionView, DataGrid dataGrid)
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

        private void CurrentSorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
        {
            Sorting(sender, e, AutoStartCollectionView, CurrentAutoStartGrid);
        }

        private void HistorySorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
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
                var AutoStartService = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
                AutoStartService.LoadCanBeAdded(autoStart);
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
                var AutoStartService = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
                AutoStartService.LoadCanBeDisabled(autoStart);
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
                var AutoStartService = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
                AutoStartService.LoadCanBeEnabled(autoStart);
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
                var AutoStartService = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
                AutoStartService.LoadCanBeRemoved(autoStart);
            }
            return false;
        }

        public static bool CanBeRevertedConverter(AutoStartEntry autoStart)
        {
            using var ServiceScope = Ioc.Default.CreateScope();
            var AutoStartService = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
            switch (autoStart.Change)
            {
                case Change.Added:
                    if (autoStart.CanBeRemoved.HasValue)
                    {
                        return autoStart.CanBeRemoved.Value;
                    }
                    if (autoStart.CanBeRemovedLoader == null)
                    {
                        AutoStartService.LoadCanBeRemoved(autoStart);
                    }
                    break;
                case Change.Removed:
                    if (autoStart.CanBeAdded.HasValue)
                    {
                        return autoStart.CanBeAdded.Value;
                    }
                    if (autoStart.CanBeAddedLoader == null)
                    {
                        AutoStartService.LoadCanBeAdded(autoStart);
                    }
                    break;
                case Change.Enabled:
                    if (autoStart.CanBeDisabled.HasValue)
                    {
                        return autoStart.CanBeDisabled.Value;
                    }
                    if (autoStart.CanBeDisabledLoader == null)
                    {
                        AutoStartService.LoadCanBeDisabled(autoStart);
                    }
                    break;
                case Change.Disabled:
                    if (autoStart.CanBeEnabled.HasValue)
                    {
                        return autoStart.CanBeEnabled.Value;
                    }
                    if (autoStart.CanBeEnabledLoader == null)
                    {
                        AutoStartService.LoadCanBeEnabled(autoStart);
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
            var AutoStartService = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartService>();
            if (autoStart.IsEnabled.Value)
            {
                if (!autoStart.CanBeDisabled.HasValue)
                {
                    AutoStartService.LoadCanBeDisabled(autoStart);
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
                    AutoStartService.LoadCanBeEnabled(autoStart);
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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SettingsPage()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
