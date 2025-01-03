// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using AutoStartConfirm.Business;
using AutoStartConfirm.Models;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Resources;

namespace AutoStartConfirm.GUI
{
    public sealed partial class MainPage : Page, ISubPage, IDisposable
    {
        private bool disposedValue = false;

        public string NavTitle { get; set; }

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

        private AdvancedCollectionView? ignoredCollectionView;

        public AdvancedCollectionView IgnoredCollectionView
        {
            get
            {
                if (ignoredCollectionView == null)
                {
                    ignoredCollectionView = new AdvancedCollectionView(AutoStartBusiness.IgnoredAutoStarts, true);
                    ignoredCollectionView.SortDescriptions.Add(new SortDescription("CategoryAsString", SortDirection.Descending));
                    ignoredCollectionView.SortDescriptions.Add(new SortDescription("Path", SortDirection.Descending));
                    ignoredCollectionView.SortDescriptions.Add(new SortDescription("Value", SortDirection.Descending));
                }
                return ignoredCollectionView;
            }
        }

        // public IEnumerable<string> CompareTypeItemSource = Enum.GetValues(typeof(CompareType)).Cast<CompareType>().Select(v => v.ToString());
        public static readonly IEnumerable<CompareType> CompareTypeItemSource = Enum.GetValues(typeof(CompareType)).Cast<CompareType>();

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            var resourceLoader = new ResourceLoader("AutoStartConfirmLib/Resources");
            NavTitle = resourceLoader.GetString("NavigationHome/Content");
        }

        public async void CurrentConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

        private async void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStart = (AutoStartEntry)button.DataContext;
            await AutoStartBusiness.IgnoreAutoStart(autoStart);
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
        private async void IgnoredRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStart = (IgnoredAutoStart)button.DataContext;
            await AutoStartBusiness.RemoveIgnoreAutoStart(autoStart);
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

        public static void Sorting(object? _, DataGridColumnEventArgs e, AdvancedCollectionView collectionView, DataGrid dataGrid)
        {
            var newSortColumn = e.Column.Tag.ToString();
            if (newSortColumn == null)
            {
                return;
            }
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

        public void CurrentSorting(object sender, DataGridColumnEventArgs e)
        {
            Sorting(sender, e, AutoStartCollectionView, CurrentAutoStartGrid);
        }

        public void HistorySorting(object sender, DataGridColumnEventArgs e)
        {
            Sorting(sender, e, HistoryAutoStartCollectionView, HistoryAutoStartGrid);
        }

        public void IgnoredSorting(object sender, DataGridColumnEventArgs e)
        {
            Sorting(sender, e, IgnoredCollectionView, IgnoredAutoStartGrid);
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

        public static bool CanBeIgnoredConverter(AutoStartEntry autoStart)
        {
            if (autoStart.CanBeIgnored.HasValue)
            {
                return autoStart.CanBeIgnored.Value;
            }
            if (autoStart.CanBeIgnoredLoader == null)
            {
                using var ServiceScope = Ioc.Default.CreateScope();
                var AutoStartBusiness = ServiceScope.ServiceProvider.GetRequiredService<IAutoStartBusiness>();
                AutoStartBusiness.LoadCanBeIgnored(autoStart);
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

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string newText = sender.Text;

            // reset the filter
            AutoStartCollectionView.Filter = _ => true;
            HistoryAutoStartCollectionView.Filter = _ => true;
            IgnoredCollectionView.Filter = _ => true;

            if (!string.IsNullOrWhiteSpace(newText))
            {
                // Because of a bug in AdvancedCollectionView, currently it is not possible to filter the last element
                // If tried, ArgumentOutOfRangeException is thrown
                // For now ignore ArgumentOutOfRangeException
                // See https://github.com/CommunityToolkit/WindowsCommunityToolkit/issues/2913
                try
                {
                    AutoStartCollectionView.Filter = x => ((AutoStartEntry)x).Value.Contains(newText, StringComparison.OrdinalIgnoreCase) || ((AutoStartEntry)x).Path.Contains(newText, StringComparison.OrdinalIgnoreCase) || ((AutoStartEntry)x).CategoryAsString.Contains(newText, StringComparison.OrdinalIgnoreCase);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                try
                {
                    HistoryAutoStartCollectionView.Filter = x => ((AutoStartEntry)x).Value.Contains(newText, StringComparison.OrdinalIgnoreCase) || ((AutoStartEntry)x).Path.Contains(newText, StringComparison.OrdinalIgnoreCase) || ((AutoStartEntry)x).CategoryAsString.Contains(newText, StringComparison.OrdinalIgnoreCase);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
                try
                {
                    IgnoredCollectionView.Filter = x => ((IgnoredAutoStart)x).Value.Contains(newText, StringComparison.OrdinalIgnoreCase) || ((IgnoredAutoStart)x).Path.Contains(newText, StringComparison.OrdinalIgnoreCase) || ((IgnoredAutoStart)x).CategoryAsString.Contains(newText, StringComparison.OrdinalIgnoreCase);
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        private CompareType? valueBeforeEdit;

        private async void IgnoredAutoStartGrid_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            try
            {
                var ignoredAutoStart = (IgnoredAutoStart)e.Row.DataContext;
                if ((string)e.Column.Tag == "ValueCompare" && valueBeforeEdit != ignoredAutoStart.ValueCompare)
                {
                    if (ignoredAutoStart.ValueCompare == CompareType.RegEx)
                    {
                        ignoredAutoStart.Value = EscapeRegex(ignoredAutoStart.Value);
                    }
                    if (valueBeforeEdit == CompareType.RegEx)
                    {
                        ignoredAutoStart.Value = UnescapeRegex(ignoredAutoStart.Value);
                    }
                }
                if ((string)e.Column.Tag == "PathCompare" && valueBeforeEdit != ignoredAutoStart.PathCompare)
                {
                    if (ignoredAutoStart.PathCompare == CompareType.RegEx)
                    {
                        ignoredAutoStart.Path = EscapeRegex(ignoredAutoStart.Path);
                    }
                    if (valueBeforeEdit == CompareType.RegEx)
                    {
                        ignoredAutoStart.Path = UnescapeRegex(ignoredAutoStart.Path);
                    }
                }
                AutoStartBusiness.UpdateIgnoredAutoStart(ignoredAutoStart);
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to save changed to ignored auto start";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }

        /// <summary>
        /// Tries to unescape regex charecters when switching from regex to non regex comparison.
        /// Returns unmodified value if the regex has invalid escaping.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string UnescapeRegex(string value)
        {
            try
            {
                var unescapedContent = Regex.Unescape(value);
                if (unescapedContent.StartsWith('^'))
                {
                    unescapedContent = unescapedContent.Substring(1);
                }
                if (unescapedContent.EndsWith('$'))
                {
                    unescapedContent = unescapedContent.Substring(0, unescapedContent.Length - 1);
                }
                return unescapedContent;
            }
            catch (Exception)
            {
                return value;
            }
        }

        /// <summary>
        /// Escapes regex charecters when switching from non regex to regex comparison.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string EscapeRegex(string value)
        {
            var escapedContent = Regex.Escape(value);
            return $"^{escapedContent}$";
        }

        private async void IgnoredAutoStartGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            try
            {
                var ignoredAutoStart = (IgnoredAutoStart)e.Row.DataContext;
                if ((string)e.Column.Tag == "ValueCompare")
                {
                    valueBeforeEdit = ignoredAutoStart.ValueCompare;
                }
                if ((string)e.Column.Tag == "PathCompare")
                {
                    valueBeforeEdit = ignoredAutoStart.PathCompare;
                }
                AutoStartBusiness.UpdateIgnoredAutoStart(ignoredAutoStart);
            }
            catch (Exception ex)
            {
                const string errmsg = "Failed to save changed to ignored auto start";
                Logger.LogError(ex, errmsg);
                await MessageService.ShowError(errmsg, ex);
            }
        }
    }
}
