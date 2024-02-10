using AutoStartConfirm.Connectors;
using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.Exceptions;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Update;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Xml.Serialization;
using static AutoStartConfirm.GUI.IMessageService;

namespace AutoStartConfirm.Business
{

    public delegate void AutoStartsChangeHandler(AutoStartEntry e);

    public class AutoStartBusiness : IDisposable, IAutoStartBusiness {
        #region Fields
        private readonly ILogger<AutoStartBusiness> Logger;

        private readonly IAutoStartConnectorService ConnectorService;

        private readonly IMessageService MessageService;

        public readonly IAppStatus AppStatus;

        public readonly IUpdateService UpdateService;

        private readonly INotificationService NotificationService;

        private string? currentExePath;

        public string CurrentExePath {
            get {
                currentExePath ??= Environment.ProcessPath!;
                return currentExePath;
            }
            set {
                currentExePath = value;
            }
        }

        private readonly string PathToLastAutoStarts;

        private readonly string PathToHistoryAutoStarts;

        private readonly string PathToIgnoredAutoStarts;

        public bool NotificationsEnabled = false;


        private readonly ObservableCollection<AutoStartEntry> currentAutoStarts = new();

        public ObservableCollection<AutoStartEntry> CurrentAutoStarts => currentAutoStarts;


        private readonly ObservableCollection<AutoStartEntry> allCurrentAutoStarts = new();

        public ObservableCollection<AutoStartEntry> AllCurrentAutoStarts => allCurrentAutoStarts;


        private readonly ObservableCollection<AutoStartEntry> historyAutoStarts = new();

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts => historyAutoStarts;


        private ObservableCollection<AutoStartEntry> allHistoryAutoStarts = new();

        public ObservableCollection<AutoStartEntry> AllHistoryAutoStarts => allHistoryAutoStarts;


        private ObservableCollection<IgnoredAutoStart> ignoredAutoStarts = new();

        public ObservableCollection<IgnoredAutoStart> IgnoredAutoStarts => ignoredAutoStarts;

        private readonly ISettingsService SettingsService;

        private readonly ICurrentUserRun64Connector CurrentUserRun64Connector;

        private readonly IDispatchService DispatchService;

        public bool HasOwnAutoStart {
            get {
                var currentAutoStarts = CurrentUserRun64Connector.GetCurrentAutoStarts();
                foreach (var autoStart in currentAutoStarts) {
                    if (IsOwnAutoStart(autoStart)) {
                        return true;
                    }
                }
                return false;
            }
        }

        private readonly IUacService UacService;

        public System.Timers.Timer SettingSaveTimer;

        public string RevertAddParameterName
        {
            get
            {
                return "--revertAdd";
            }
        }

        public string RevertRemoveParameterName
        {
            get
            {
                return "--revertRemove";
            }
        }

        public string EnableParameterName
        {
            get
            {
                return "--enable";
            }
        }

        public string DisableParameterName
        {
            get
            {
                return "--disable";
            }
        }

        #endregion

        #region Methods

        public AutoStartBusiness(
            ILogger<AutoStartBusiness> logger,
            IAutoStartConnectorService connectorService,
            ISettingsService settingsService,
            ICurrentUserRun64Connector currentUserRun64Connector,
            IDispatchService dispatchService,
            IUacService uacService,
            IMessageService messageService,
            IAppStatus appStatus,
            IUpdateService updateService,
            INotificationService notificationService
        ) {
            Logger = logger;
            ConnectorService = connectorService;
            SettingsService = settingsService;
            MessageService = messageService;
            CurrentUserRun64Connector = currentUserRun64Connector;
            DispatchService = dispatchService;
            UacService = uacService;
            AppStatus = appStatus;
            UpdateService = updateService;
            NotificationService = notificationService;
            SettingSaveTimer = new(1000)
            {
                AutoReset = false
            };
            SettingSaveTimer.Elapsed += SettingSaveTimer_Elapsed;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}";
            ConnectorService.Add += AddHandler;
            ConnectorService.Remove += RemoveHandler;
            ConnectorService.Enable += EnableHandler;
            ConnectorService.Disable += DisableHandler;
            PathToLastAutoStarts = $"{basePath}LastAutoStarts";
            PathToHistoryAutoStarts = $"{basePath}HistoryAutoStarts";
            PathToIgnoredAutoStarts = $"{basePath}IgnoredAutoStarts";
            SettingsService.SettingsSaving += SettingsSavingHandler;
            SettingsService.SettingsLoaded += SettingsLoadedHandler;
        }

        public void Run()
        {
            // disable notifications for new added auto starts on first start to avoid too many notifications at once
            bool isFirstRun = GetValidAutoStartFileExists();
            if (!isFirstRun)
            {
                NotificationsEnabled = true;
            }

            try
            {
                LoadCurrentAutoStarts();
                AppStatus.HasOwnAutoStart = HasOwnAutoStart;
            }
            catch (Exception)
            {
            }

            if (isFirstRun)
            {
                NotificationsEnabled = true;
            }
            StartWatcher();

            if (SettingsService.CheckForUpdatesOnStart)
            {
                UpdateService.CheckUpdateAndShowNotification();
            }
        }

        private void SettingSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            SaveAutoStarts();
            SettingsService.Save();
        }

        private void SettingsLoadedHandler(object sender, SettingsLoadedEventArgs e)
        {
            Logger.LogTrace("SettingsLoadedHandler called");
            HandleSettingChanges();
        }

        private void SettingsSavingHandler(object sender, CancelEventArgs e)
        {
            Logger.LogTrace("SettingsSavingHandler called");
            HandleSettingChanges();
        }

        private void HandleSettingChanges()
        {
            Logger.LogTrace("HandleSettingChanges called");
            foreach (var autoStart in CurrentAutoStarts.ToList())
            {
                if (SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString()))
                {
                    CurrentAutoStarts.Remove(autoStart);
                }
            }
            foreach (var autoStart in AllCurrentAutoStarts.ToList())
            {
                if (!SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString()) && !CurrentAutoStarts.Contains(autoStart))
                {
                    CurrentAutoStarts.Add(autoStart);
                }
            }

            foreach (var autoStart in HistoryAutoStarts.ToList())
            {
                if (SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString()))
                {
                    HistoryAutoStarts.Remove(autoStart);
                }
            }
            foreach (var autoStart in AllHistoryAutoStarts.ToList())
            {
                if (!SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString()) && !HistoryAutoStarts.Contains(autoStart))
                {
                    HistoryAutoStarts.Add(autoStart);
                }
            }
            Logger.LogTrace("HandleSettingChanges completed");
        }

        public bool TryGetCurrentAutoStart(Guid Id, [NotNullWhen(returnValue: true)] out AutoStartEntry? value) {
            Logger.LogTrace("TryGetCurrentAutoStart called for {AutoStartId}", Id);
            value = null;
            foreach (var autoStart in AllCurrentAutoStarts) {
                if (autoStart.Id == Id) {
                    value = autoStart;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetHistoryAutoStart(Guid Id, [NotNullWhen(returnValue: true)] out AutoStartEntry? value) {
            Logger.LogTrace("TryGetHistoryAutoStart called for {AutoStartId}", Id);
            value = null;
            foreach (var autoStart in AllHistoryAutoStarts) {
                if (autoStart.Id == Id) {
                    value = autoStart;
                    return true;
                }
            }
            return false;
        }

        #region AutoStart changes

        public async Task ConfirmAdd(Guid Id) {
            await Task.Run(() => {
                Logger.LogTrace("ConfirmAdd called for {AutoStartId}", Id);
                if (TryGetHistoryAutoStart(Id, out AutoStartEntry? addedAutoStart))
                {
                    addedAutoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                }
                if (TryGetCurrentAutoStart(Id, out AutoStartEntry? currentAutoStart))
                {
                    currentAutoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                    //Confirm?.Invoke(currentAutoStart);
                    Logger.LogInformation("Confirmed add of {@addedAutoStart}", addedAutoStart);
                }
            });
        }

        public async Task ConfirmRemove(Guid Id)
        {
            await Task.Run(() => {
                Logger.LogTrace("ConfirmRemove called for {AutoStartId}", Id);
                if (TryGetHistoryAutoStart(Id, out AutoStartEntry? autoStart)) {
                    autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                    Logger.LogInformation("Confirmed remove of {@autoStart}", autoStart);
                }
            });
        }

        public async Task ConfirmAdd(AutoStartEntry autoStart) {
            autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            await ConfirmAdd(autoStart.Id);
        }

        public async Task ConfirmRemove(AutoStartEntry autoStart) {
            autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            await ConfirmRemove(autoStart.Id);
        }

        public async Task RemoveAutoStart(Guid Id, bool showDialogsAndCatchErrors = true) {
            Logger.LogTrace("RemoveAutoStart called for {AutoStartId}", Id);
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry? autoStart)) {
                await RemoveAutoStart(autoStart, showDialogsAndCatchErrors);
            }
            else
            {
                const string message = "AutoStart not found";
                Logger.LogError("AutoStart {id} not found", Id);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message);
                }
                else
                {
                    throw new Exception(message);
                }
            }
        }

        public async Task RemoveAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true)
        {
            try
            {
                AppStatus.IncrementRunningActionCount();
                Logger.LogTrace("RemoveAutoStart called for {AutoStartId}", autoStart.Id);
                if (showDialogsAndCatchErrors && !await MessageService.ShowConfirm(autoStart, AutoStartAction.Remove))
                {
                    return;
                }
                if (IsAdminRequiredForChanges(autoStart) && !UacService.IsProcessElevated)
                {
                    await StartSubProcessAsAdmin(autoStart, RevertAddParameterName);
                }
                else
                {
                    if (ConnectorService.CanBeEnabled(autoStart))
                    {
                        // remove disabled status to allow new entries for example at the same registry key in the future
                        ConnectorService.EnableAutoStart(autoStart);
                    }
                    ConnectorService.RemoveAutoStart(autoStart);
                }
                autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                Logger.LogInformation("Removed {@autoStart}", autoStart);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowSuccess(autoStart, AutoStartAction.Remove);
                }
            }
            catch (Exception e)
            {
                const string message = "Failed to remove auto start";
                Logger.LogError(e, message);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message, e);
                }
                else
                {
                    throw new Exception(message, e);
                }
            }
            finally
            {
                AppStatus.DecrementRunningActionCount();
            }
        }

        public async Task DisableAutoStart(Guid Id, bool showDialogsAndCatchErrors = true) {
            Logger.LogTrace("DisableAutoStart called for {AutoStartId}", Id);
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry? autoStart)) {
                await DisableAutoStart(autoStart, showDialogsAndCatchErrors);
            }
            else
            {
                const string message = "AutoStart not found";
                Logger.LogError("AutoStart {id} not found", Id);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message);
                }
                else
                {
                    throw new Exception(message);
                }
            }
        }

        public async Task DisableAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true)
        {
            try
            {
                AppStatus.IncrementRunningActionCount();
                Logger.LogTrace("DisableAutoStart called for {AutoStartId}", autoStart.Id);
                if (showDialogsAndCatchErrors && !await MessageService.ShowConfirm(autoStart, AutoStartAction.Disable))
                {
                    return;
                }
                if (IsAdminRequiredForChanges(autoStart) && !UacService.IsProcessElevated)
                {
                    await StartSubProcessAsAdmin(autoStart, DisableParameterName);
                }
                else
                {
                    ConnectorService.DisableAutoStart(autoStart);
                }
                autoStart.ConfirmStatus = ConfirmStatus.Disabled;
                Logger.LogInformation("Disabled {@autoStart}", autoStart);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowSuccess(autoStart, AutoStartAction.Disable);
                }
            }
            catch (Exception e)
            {
                const string message = "Failed to disable auto start";
                Logger.LogError(e, message);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message, e);
                }
                else
                {
                    throw new Exception(message, e);
                }
            }
            finally
            {
                AppStatus.DecrementRunningActionCount();
            }
        }

        public async Task AddAutoStart(Guid Id, bool showDialogsAndCatchErrors = true) {
            Logger.LogTrace("AddAutoStart called for {AutoStartId}", Id);
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry? autoStart)) {
                await AddAutoStart(autoStart, showDialogsAndCatchErrors);
            }
            else
            {
                const string message = "AutoStart not found";
                Logger.LogError("AutoStart {id} not found", Id);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message);
                }
                else
                {
                    throw new Exception(message);
                }
            }
        }

        public async Task AddAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true)
        {
            try
            {
                AppStatus.IncrementRunningActionCount();
                Logger.LogTrace("AddAutoStart called for {AutoStartId}", autoStart.Id);
                if (showDialogsAndCatchErrors && !await MessageService.ShowConfirm(autoStart, AutoStartAction.Add))
                {
                    return;
                }
                if (IsAdminRequiredForChanges(autoStart) && !UacService.IsProcessElevated)
                {
                    await StartSubProcessAsAdmin(autoStart, RevertRemoveParameterName);
                }
                else
                {
                    ConnectorService.AddAutoStart(autoStart);
                    try
                    {
                        ConnectorService.EnableAutoStart(autoStart);
                    }
                    catch (AlreadySetException)
                    {

                    }
                }
                autoStart.ConfirmStatus = ConfirmStatus.Reverted;
                Logger.LogInformation("Added {@autoStart}", autoStart);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowSuccess(autoStart, AutoStartAction.Add);
                }
            }
            catch (Exception e)
            {
                const string message = "Failed to add auto start";
                Logger.LogError(e, message);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message, e);
                }
                else
                {
                    throw new Exception(message, e);
                }
            }
            finally
            {
                AppStatus.DecrementRunningActionCount();
            }
        }

        public async Task EnableAutoStart(Guid Id, bool showDialogsAndCatchErrors = true) {
            Logger.LogTrace("EnableAutoStart called for {AutoStartId}", Id);
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry? autoStart)) {
                await EnableAutoStart(autoStart, showDialogsAndCatchErrors);
            }
            else
            {
                const string message = "AutoStart not found";
                Logger.LogError("AutoStart {id} not found", Id);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message);
                }
                else
                {
                    throw new Exception(message);
                }
            }
        }

        public async Task EnableAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true)
        {
            try
            {
                AppStatus.IncrementRunningActionCount();
                Logger.LogTrace("EnableAutoStart called for {AutoStartId}", autoStart.Id);
                if (showDialogsAndCatchErrors && !await MessageService.ShowConfirm(autoStart, AutoStartAction.Enable))
                {
                    return;
                }
                // todo: ask for required parameters, like start type of services
                if (IsAdminRequiredForChanges(autoStart) && !UacService.IsProcessElevated)
                {
                    await StartSubProcessAsAdmin(autoStart, EnableParameterName);
                }
                else
                {
                    ConnectorService.EnableAutoStart(autoStart);
                }
                autoStart.ConfirmStatus = ConfirmStatus.Enabled;
                Logger.LogInformation("Enabled {@autoStart}", autoStart);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowSuccess(autoStart, AutoStartAction.Enable);
                }
            }
            catch (Exception e)
            {
                const string message = "Failed to enable auto start";
                Logger.LogError(e, message);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message, e);
                }
                else
                {
                    throw new Exception(message, e);
                }
            }
            finally
            {
                AppStatus.DecrementRunningActionCount();
            }
        }

        public async Task ToggleOwnAutoStart(bool showDialogsAndCatchErrors = true)
        {
            try
            {
                AppStatus.IncrementRunningActionCount();
                Logger.LogInformation("ToggleOwnAutoStart called");
                var ownAutoStart = new RegistryAutoStartEntry()
                {
                    Category = Category.CurrentUserRun64,
                    Path = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm",
                    Value = CurrentExePath,
                    RegistryValueKind = Microsoft.Win32.RegistryValueKind.String,
                    ConfirmStatus = ConfirmStatus.New,
                };

                AutoStartAction action = HasOwnAutoStart ? AutoStartAction.Remove : AutoStartAction.Add;
                if (HasOwnAutoStart)
                {
                    if (showDialogsAndCatchErrors && !await MessageService.ShowConfirm(ownAutoStart, action))
                    {
                        return;
                    }
                    Logger.LogInformation("Shall remove own auto start");
                    await RemoveAutoStart(ownAutoStart, false);
                }
                else
                {
                    if (showDialogsAndCatchErrors && !await MessageService.ShowConfirm(ownAutoStart, action))
                    {
                        return;
                    }
                    Logger.LogInformation("Shall add own auto start");
                    await AddAutoStart(ownAutoStart, false);
                }
                ownAutoStart.ConfirmStatus = ConfirmStatus.New;
                Logger.LogTrace("Own auto start toggled");
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowSuccess(ownAutoStart, action);
                }
            }
            catch (Exception e)
            {
                const string message = "Failed to change own auto start";
                Logger.LogError(e, message);
                if (showDialogsAndCatchErrors)
                {
                    await MessageService.ShowError(message, e);
                }
                else
                {
                    throw new Exception(message, e);
                }
            }
            finally
            {
                AppStatus.DecrementRunningActionCount();
            }
        }

        public async Task IgnoreAutoStart(Guid Id)
        {
            Logger.LogTrace("IgnoreAutoStart called for {AutoStartId}", Id);
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry? autoStart))
            {
                await IgnoreAutoStart(autoStart);
            }
            else
            {
                const string message = "AutoStart not found";
                Logger.LogError("AutoStart {id} not found", Id);
                await MessageService.ShowError(message);
            }
        }

        public async Task IgnoreAutoStart(AutoStartEntry autoStart)
        {
            try
            {
                AppStatus.IncrementRunningActionCount();
                Logger.LogTrace("IgnoreAutoStart called for {AutoStartId}", autoStart.Id);
                if (!await MessageService.ShowConfirm(autoStart, AutoStartAction.Ignore))
                {
                    return;
                }
                var ignoredAutoStart = new IgnoredAutoStart(autoStart);
                IgnoredAutoStarts.Add(ignoredAutoStart);
                ResetIgnorePropertiesOfAutoStarts(ignoredAutoStart);
                autoStart.CanBeIgnored = false;
                Logger.LogInformation("Ignored {@autoStart}", autoStart);
                SettingSaveTimer.Start();
                await MessageService.ShowSuccess(autoStart, AutoStartAction.Ignore);
            }
            catch (Exception e)
            {
                const string message = "Failed to ignore auto start";
                Logger.LogError(e, message);
                await MessageService.ShowError(message, e);
            }
            finally
            {
                AppStatus.DecrementRunningActionCount();
            }
        }

        public async Task RemoveIgnoreAutoStart(IgnoredAutoStart ignoredAutoStart)
        {
            try
            {
                AppStatus.IncrementRunningActionCount();
                Logger.LogTrace("RemoveIgnoreAutoStart called for {AutoStart}", ignoredAutoStart);
                if (!await MessageService.ShowRemoveConfirm(ignoredAutoStart))
                {
                    return;
                }

                await DispatchService.EnqueueAsync(async () =>
                {
                    if (IgnoredAutoStarts.Count == 1 && IgnoredAutoStarts.Contains(ignoredAutoStart))
                    {
                        // Handle off by one error in remove
                        IgnoredAutoStarts.Clear();
                    }
                    else
                    {
                        IgnoredAutoStarts.Remove(ignoredAutoStart);
                    }
                    ResetIgnorePropertiesOfAutoStarts(ignoredAutoStart);
                    Logger.LogInformation("Removed ignored {@autoStart}", ignoredAutoStart);
                    SettingSaveTimer.Start();
                    await MessageService.ShowRemoveSuccess(ignoredAutoStart);
                }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);
            }
            catch (Exception e)
            {
                const string message = "Failed to ignore auto start";
                Logger.LogError(e, message);
                await MessageService.ShowError(message, e);
            }
            finally
            {
                AppStatus.DecrementRunningActionCount();
            }
        }

        #endregion

        public bool CanAutoStartBeEnabled(AutoStartEntry autoStart) {
            return ConnectorService.CanBeEnabled(autoStart);
        }

        public bool CanAutoStartBeDisabled(AutoStartEntry autoStart) {
            return ConnectorService.CanBeDisabled(autoStart);
        }

        public bool CanAutoStartBeAdded(AutoStartEntry autoStart) {
            return ConnectorService.CanBeAdded(autoStart);
        }

        public bool CanAutoStartBeRemoved(AutoStartEntry autoStart) {
            return ConnectorService.CanBeRemoved(autoStart);
        }

        public bool CanAutoStartBeIgnored(AutoStartEntry autoStart)
        {
            foreach (var ignoredAutoStart in IgnoredAutoStarts)
            {
                if (IsAutoStartIgnored(autoStart, ignoredAutoStart))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsAutoStartIgnored(AutoStartEntry autoStart, IgnoredAutoStart ignoredAutoStart)
        {
            return ignoredAutoStart.Category == autoStart.Category &&
                   ignoredAutoStart.Path.Equals(autoStart.Path, StringComparison.OrdinalIgnoreCase) &&
                   ignoredAutoStart.Value.Equals(autoStart.Value, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> LoadCanBeAdded(AutoStartEntry autoStart)
        {
            lock (autoStart.LoaderLock)
            {
                autoStart.CanBeAddedLoader ??= Task<bool>.Run(() => {
                    var oldValue = autoStart.CanBeAdded;
                    var newValue = CanAutoStartBeAdded(autoStart);
                    if (oldValue != newValue)
                    {
                        autoStart.CanBeAdded = newValue;
                    }
                    return newValue;
                });
            }
            var newValue = await autoStart.CanBeAddedLoader;
            return newValue;
        }

        public async Task<bool> LoadCanBeRemoved(AutoStartEntry autoStart)
        {
            lock (autoStart.LoaderLock)
            {
                autoStart.CanBeRemovedLoader ??= Task<bool>.Run(() => {
                    var oldValue = autoStart.CanBeRemoved;
                    var newValue = CanAutoStartBeRemoved(autoStart);
                    if (oldValue != newValue)
                    {
                        autoStart.CanBeRemoved = newValue;
                    }
                    return newValue;
                });
            }
            var newValue = await autoStart.CanBeRemovedLoader;
            return newValue;
        }

        public async Task<bool> LoadCanBeIgnored(AutoStartEntry autoStart)
        {
            lock (autoStart.LoaderLock)
            {
                autoStart.CanBeIgnoredLoader ??= Task<bool>.Run(() => {
                    var oldValue = autoStart.CanBeIgnored;
                    var newValue = CanAutoStartBeIgnored(autoStart);
                    if (oldValue != newValue)
                    {
                        autoStart.CanBeIgnored = newValue;
                    }
                    return newValue;
                });
            }
            var newValue = await autoStart.CanBeIgnoredLoader;
            return newValue;
        }

        public async Task<bool> LoadCanBeEnabled(AutoStartEntry autoStart)
        {
            lock (autoStart.LoaderLock)
            {
                autoStart.CanBeEnabledLoader ??= Task<bool>.Run(() => {
                    var oldValue = autoStart.CanBeEnabled;
                    var newValue = CanAutoStartBeEnabled(autoStart);
                    if (oldValue != newValue)
                    {
                        autoStart.CanBeEnabled = newValue;
                    }
                    return newValue;
                });
            }
            var newValue = await autoStart.CanBeEnabledLoader;
            return newValue;
        }

        public async Task<bool> LoadCanBeDisabled(AutoStartEntry autoStart)
        {
            lock(autoStart.LoaderLock)
            {
                autoStart.CanBeDisabledLoader ??= Task<bool>.Run(() => {
                    var oldValue = autoStart.CanBeDisabled;
                    var newValue = CanAutoStartBeDisabled(autoStart);
                    if (oldValue != newValue)
                    {
                        autoStart.CanBeDisabled = newValue;
                    }
                    return newValue;
                });
            }
            var newValue = await autoStart.CanBeDisabledLoader;
            return newValue;
        }

        /// <summary>
        /// Resets all ignore properties of all known auto starts affected by the given one
        /// </summary>
        /// <param name="ignoredAutoStart"></param>
        public void ResetIgnorePropertiesOfAutoStarts(IgnoredAutoStart ignoredAutoStart)
        {
            foreach (var autoStart in AllCurrentAutoStarts)
            {
                if (IsAutoStartIgnored(autoStart, ignoredAutoStart)) {
                    autoStart.CanBeIgnored = null;
                    autoStart.CanBeIgnoredLoader = null;
                }
            }
            foreach (var autoStart in AllHistoryAutoStarts)
            {
                if (IsAutoStartIgnored(autoStart, ignoredAutoStart)) {
                    autoStart.CanBeIgnored = null;
                    autoStart.CanBeIgnoredLoader = null;
                }
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all known auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfAutoStarts(AutoStartEntry autoStart) {
            ResetEditablePropertiesOfCurrentAutoStarts(autoStart);
            ResetEditablePropertiesOfHistoryAutoStarts(autoStart);
        }

        /// <summary>
        /// Resets all dynamic properties of all current auto starts.
        /// </summary>
        public void ResetEditablePropertiesOfAllCurrentAutoStarts() {
            foreach (var autoStart in CurrentAutoStarts) {
                var autoStartValue = autoStart;
                ResetAllDynamicFields(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all current auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfCurrentAutoStarts(AutoStartEntry autoStart) {
            foreach (var currentAutoStart in AllCurrentAutoStarts) {
                var autoStartValue = currentAutoStart;
                if (autoStartValue.Category != autoStart.Category ||
                    !autoStartValue.Path.Equals(autoStart.Path, StringComparison.OrdinalIgnoreCase) ||
                    !autoStartValue.Value.Equals(autoStart.Value, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                ResetAllDynamicFields(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all history auto starts.
        /// </summary>
        public void ResetEditablePropertiesOfAllHistoryAutoStarts() {
            foreach (var autoStart in AllHistoryAutoStarts) {
                ResetAllDynamicFields(autoStart);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all history auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfHistoryAutoStarts(AutoStartEntry autoStart) {
            foreach (var historyAutoStart in AllHistoryAutoStarts) {
                if (historyAutoStart.Category != autoStart.Category || 
                    !historyAutoStart.Path.Equals(autoStart.Path, StringComparison.OrdinalIgnoreCase) ||
                    !historyAutoStart.Value.Equals(autoStart.Value, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                ResetAllDynamicFields(historyAutoStart);
            }
        }

        private static void ResetAllDynamicFields(AutoStartEntry autoStartValue) {
            autoStartValue.CanBeAdded = null;
            autoStartValue.CanBeRemoved = null;
            autoStartValue.CanBeEnabled = null;
            autoStartValue.CanBeDisabled = null;
            autoStartValue.CanBeIgnored = null;
            autoStartValue.CanBeAddedLoader = null;
            autoStartValue.CanBeRemovedLoader = null;
            autoStartValue.CanBeEnabledLoader = null;
            autoStartValue.CanBeDisabledLoader = null;
            autoStartValue.CanBeIgnoredLoader = null;
        }

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            Logger.LogTrace("IsAdminRequiredForChanges called");
            return ConnectorService.IsAdminRequiredForChanges(autoStart);
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.LogTrace("GetCurrentAutoStarts called");
            return ConnectorService.GetCurrentAutoStarts();
        }

        public bool GetValidAutoStartFileExists() {
            Logger.LogTrace("GetAutoStartFileExists called");
            if (!File.Exists(PathToLastAutoStarts)) {
                return false;
            }
            try {
                GetSavedAutoStarts<AutoStartEntry>(PathToLastAutoStarts);
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public ObservableCollection<T> GetSavedAutoStarts<T>(string path) {
            try {
                Logger.LogTrace("Loading auto starts from file {path}", path);
                ObservableCollection<T>? ret = null;
                if (File.Exists($"{path}.xml"))
                {
                    var file = $"{path}.xml";
                    Logger.LogInformation("Loading new xml serialized file {file}", file);
                    using Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    XmlSerializer serializer = new(typeof(ObservableCollection<T>));
                    try
                    {
                        ret = (ObservableCollection<T>?)serializer.Deserialize(stream);
                        Logger.LogTrace("Loaded last saved auto starts from file {file}", file);
                    }
                    catch (Exception ex)
                    {
                        var err = new Exception($"Failed to deserialize from file {file}", ex);
                        throw err;
                    }
                }
                return ret ?? new ObservableCollection<T>();
            } catch (Exception ex) {
                const string message = "Failed to load last auto starts";
                Logger.LogError(ex, message);
                throw new Exception(message, ex); ;
            }
        }

        public void SaveAutoStarts() {
            try {
                Logger.LogInformation("Saving current known auto starts");
                Logger.LogTrace("Saving current auto starts to file {path}", PathToLastAutoStarts);
                SaveAutoStarts(PathToLastAutoStarts, AllCurrentAutoStarts);
                Logger.LogTrace("Saving history auto starts to file {path}", PathToHistoryAutoStarts);
                SaveAutoStarts(PathToHistoryAutoStarts, AllHistoryAutoStarts);
                Logger.LogTrace("Saving ignored auto starts to file {path}", PathToIgnoredAutoStarts);
                SaveAutoStarts(PathToIgnoredAutoStarts, IgnoredAutoStarts);
                Logger.LogInformation("Saved all auto starts");
            } catch (Exception ex) {
                const string message = "Failed to save current auto starts";
                Logger.LogError(ex, message);
                throw new Exception(message, ex); ;
            }
        }

        private void SaveAutoStarts(string path, ObservableCollection<AutoStartEntry> dictionary) {
            Logger.LogTrace("Saving auto starts to file {path}", path);
            try {
                try {
                    var folderPath = PathToLastAutoStarts[..path.LastIndexOf(Path.DirectorySeparatorChar)];
                    Directory.CreateDirectory(folderPath);
                } catch (Exception ex) {
                    var err = new Exception($"Failed to create folder for file {path}", ex);
                    throw err;
                }
                try {
                    using Stream stream = new FileStream($"{path}.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                    XmlSerializer serializer = new(typeof(ObservableCollection<AutoStartEntry>));
                    serializer.Serialize(stream, dictionary);
                } catch (Exception ex) {
                    var err = new Exception($"Failed to write file {path}", ex);
                    throw err;
                }
                Logger.LogTrace("Saved auto starts to file {path}", path);
            } catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save auto starts to file {path}", path);
                throw new Exception($"Failed to save auto starts to file {path}", ex);
            }
        }

        private void SaveAutoStarts(string path, ObservableCollection<IgnoredAutoStart> dictionary)
        {
            Logger.LogTrace("Saving ignored auto starts to file {path}", path);
            try
            {
                try
                {
                    var folderPath = PathToLastAutoStarts[..path.LastIndexOf(Path.DirectorySeparatorChar)];
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    var err = new Exception($"Failed to create folder for file {path}", ex);
                    throw err;
                }
                try
                {
                    using Stream stream = new FileStream($"{path}.xml", FileMode.Create, FileAccess.Write, FileShare.None);
                    XmlSerializer serializer = new(typeof(ObservableCollection<IgnoredAutoStart>));
                    serializer.Serialize(stream, dictionary);
                }
                catch (Exception ex)
                {
                    var err = new Exception($"Failed to write file {path}", ex);
                    throw err;
                }
                Logger.LogTrace("Saved ignored auto starts to file {path}", path);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save ignored auto starts to file {path}", path);
                throw new Exception($"Failed to save ignored  auto starts to file {path}", ex);
            }
        }

        /// <summary>
        /// Loads current autostarts, compares to last saved and fires add or remove events if necessary
        /// </summary>
        public void LoadCurrentAutoStarts() {
            try {
                Logger.LogInformation("Comparing current auto starts to last saved");

                // get last saved auto starts
                ObservableCollection<AutoStartEntry> lastSavedAutoStarts;
                try {
                    lastSavedAutoStarts = GetSavedAutoStarts<AutoStartEntry>(PathToLastAutoStarts);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Failed to load last saved auto starts");
                    lastSavedAutoStarts = new ObservableCollection<AutoStartEntry>();
                }
                AllCurrentAutoStarts.Clear();
                var lastSavedAutoStartsDictionary = new Dictionary<Guid, AutoStartEntry>();
                foreach(var lastSavedAutoStart in lastSavedAutoStarts) {
                    if (lastSavedAutoStart.Date == null) {
                        lastSavedAutoStart.Date = DateTime.Now;
                    }
                    lastSavedAutoStartsDictionary.Add(lastSavedAutoStart.Id, lastSavedAutoStart);
                    if (SettingsService.DisabledConnectors.Contains(lastSavedAutoStart.Category.ToString())) {
                        AllCurrentAutoStarts.Add(lastSavedAutoStart);
                    }
                }

                // get history auto starts
                try {
                    allHistoryAutoStarts = GetSavedAutoStarts<AutoStartEntry>(PathToHistoryAutoStarts);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Failed to load auto starts history");
                    allHistoryAutoStarts = new ObservableCollection<AutoStartEntry>();
                }
                HistoryAutoStarts.Clear();
                foreach (var lastAutostart in allHistoryAutoStarts) {
                    if (!SettingsService.DisabledConnectors.Contains(lastAutostart.Category.ToString())) {
                        HistoryAutoStarts.Add(lastAutostart);
                    }
                }

                try
                {
                    ignoredAutoStarts = GetSavedAutoStarts<IgnoredAutoStart>(PathToIgnoredAutoStarts);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to load ignored auto starts");
                    ignoredAutoStarts = new ObservableCollection<IgnoredAutoStart>();
                }

                // get current auto starts
                IList<AutoStartEntry> currentAutoStarts;
                try {
                    currentAutoStarts = GetCurrentAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to get current auto starts", ex);
                    throw err;
                }
                var currentAutoStartDictionary = new Dictionary<Guid, AutoStartEntry>();
                foreach (var currentAutoStart in currentAutoStarts) {
                    foreach (var lastAutoStart in lastSavedAutoStarts) {
                        if (currentAutoStart.Equals(lastAutoStart)) {
                            currentAutoStart.Id = lastAutoStart.Id;
                            break;
                        }
                    }
                    currentAutoStartDictionary.Add(currentAutoStart.Id, currentAutoStart);
                }

                // get auto starts to remove
                var autoStartsToRemove = new List<AutoStartEntry>();
                foreach (var lastAutostart in lastSavedAutoStarts) {
                    if (!currentAutoStartDictionary.ContainsKey(lastAutostart.Id) &&
                        !SettingsService.DisabledConnectors.Contains(lastAutostart.Category.ToString())) {
                        autoStartsToRemove.Add(lastAutostart);
                    }
                }

                // get auto starts to add
                var autoStartsToAdd = new List<AutoStartEntry>();
                CurrentAutoStarts.Clear();
                foreach (var currentAutoStart in currentAutoStarts) {
                    if (lastSavedAutoStartsDictionary.TryGetValue(currentAutoStart.Id, out AutoStartEntry? lastAutoStartEntry)) {
                        CurrentAutoStarts.Add(lastAutoStartEntry);
                        AllCurrentAutoStarts.Add(lastAutoStartEntry);
                    } else {
                        // add handler will add auto start later to CurrentAutoStarts collection
                        autoStartsToAdd.Add(currentAutoStart);
                    }
                }

                // call remove handlers
                foreach (var removedAutoStart in autoStartsToRemove) {
                    RemoveHandler(removedAutoStart);
                }

                // call add handlers
                foreach (var addedAutoStart in autoStartsToAdd) {
                    AddHandler(addedAutoStart);
                }

                // call enable / disable handlers
                foreach (var currentAutoStart in currentAutoStarts) {
                    bool wasEnabled = true;
                    if (lastSavedAutoStartsDictionary.TryGetValue(currentAutoStart.Id, out AutoStartEntry? oldAutoStart)) {
                        wasEnabled = oldAutoStart.IsEnabled.GetValueOrDefault(true);
                    }
                    var nowEnabled = ConnectorService.IsEnabled(currentAutoStart);
                    currentAutoStart.IsEnabled = nowEnabled;
                    if (nowEnabled && !wasEnabled) {
                        EnableHandler(currentAutoStart);
                    } else if (!nowEnabled && wasEnabled) {
                        DisableHandler(currentAutoStart);
                    }
                }
                Logger.LogTrace("LoadCurrentAutoStarts finished");
            } catch (Exception ex) {
                const string message = "Failed to compare current auto starts to last saved";
                Logger.LogError(ex, message);
                throw new Exception(message, ex);
            }
        }

        public void StartWatcher() {
            Logger.LogInformation("Starting watchers");
            ConnectorService.StartWatcher();
            Logger.LogInformation("Started watchers");
        }

        public void StopWatcher() {
            Logger.LogTrace("Stopping watchers");
            ConnectorService.StopWatcher();
            Logger.LogTrace("Stopped watchers");
        }

        public bool IsOwnAutoStart(AutoStartEntry autoStart) {
            return autoStart.Category == Category.CurrentUserRun64 &&
            autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
            autoStart.Value == CurrentExePath;
        }

        // todo: write tests for calls
        private async Task StartSubProcessAsAdmin(AutoStartEntry autoStart, string parameterName)
        {
            Logger.LogInformation("Starting elevated sub process");
            string path = Path.GetTempFileName();
            try
            {
                using (Stream stream = new FileStream($"{path}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    XmlSerializer serializer = new(typeof(AutoStartEntry));
                    serializer.Serialize(stream, autoStart);
                }

                var info = new ProcessStartInfo(
                    CurrentExePath,
                    $"{parameterName} {path}")
                {
                    Verb = "runas", // indicates to elevate privileges
                    UseShellExecute = true, // needed if running in newer .NET version as 4 to elevate process
                };

                var process = new Process
                {
                    EnableRaisingEvents = true, // enable WaitForExit()
                    StartInfo = info
                };

                process.Start();
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    throw new Exception("Sub process failed to execute");
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        public async Task ClearHistory()
        {
            Logger.LogTrace("Clear history called");
            if (!await MessageService.ShowConfirm("Clear history", "Are you sure to delete all recorded auto start changes?"))
            {
                Logger.LogTrace("Clear history aborted");
                return;
            }
            Logger.LogInformation("Clearing history");
            HistoryAutoStarts.Clear();
            AllHistoryAutoStarts.Clear();
            SaveAutoStarts(PathToHistoryAutoStarts, AllHistoryAutoStarts);
            Logger.LogTrace("History cleared");
        }

        #endregion

        #region Event handlers
        private void AddHandler(AutoStartEntry autostart) {
            DispatchService.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start added: {@value}", autostart);
                    autostart.Date = DateTime.Now;
                    autostart.Change = Change.Added;
                    ResetEditablePropertiesOfAutoStarts(autostart);
                    CurrentAutoStarts.Add(autostart);
                    AllCurrentAutoStarts.Add(autostart);
                    HistoryAutoStarts.Add(autostart);
                    AllHistoryAutoStarts.Add(autostart);
                    if (IsOwnAutoStart(autostart))
                    {
                        Logger.LogInformation("Own auto start added");
                        AppStatus.HasOwnAutoStart = true;
                    }
                    if (NotificationsEnabled && CanAutoStartBeIgnored(autostart))
                    {
                        NotificationService.ShowNewAutoStartEntryNotification(autostart);
                    }
                    SettingSaveTimer.Start();
                    Logger.LogTrace("AddHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Add handler failed");
                }
            });
        }

        private void EnableHandler(AutoStartEntry autostart)
        {
            DispatchService.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start enabled: {@value}", autostart);
                    ResetEditablePropertiesOfAutoStarts(autostart);
                    var autostartCopy = autostart.DeepCopy();
                    autostartCopy.Date = DateTime.Now;
                    autostartCopy.Change = Change.Enabled;
                    AllCurrentAutoStarts.Remove(autostart);
                    CurrentAutoStarts.Remove(autostart);
                    AllCurrentAutoStarts.Add(autostartCopy);
                    CurrentAutoStarts.Add(autostartCopy);
                    AllHistoryAutoStarts.Add(autostartCopy);
                    HistoryAutoStarts.Add(autostartCopy);
                    if (NotificationsEnabled && CanAutoStartBeIgnored(autostart))
                    {
                        NotificationService.ShowEnabledAutoStartEntryNotification(autostart);
                    }
                    SettingSaveTimer.Start();
                    Logger.LogTrace("EnableHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Enable handler failed");
                }
            });
        }

        private void DisableHandler(AutoStartEntry autostart)
        {
            DispatchService.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start disabled: {@value}", autostart);
                    ResetEditablePropertiesOfAutoStarts(autostart);
                    var autostartCopy = autostart.DeepCopy();
                    autostartCopy.Date = DateTime.Now;
                    autostartCopy.Change = Change.Disabled;
                    AllCurrentAutoStarts.Remove(autostart);
                    CurrentAutoStarts.Remove(autostart);
                    AllCurrentAutoStarts.Add(autostartCopy);
                    CurrentAutoStarts.Add(autostartCopy);
                    AllHistoryAutoStarts.Add(autostartCopy);
                    HistoryAutoStarts.Add(autostartCopy);
                    if (NotificationsEnabled && CanAutoStartBeIgnored(autostart))
                    {
                        NotificationService.ShowDisabledAutoStartEntryNotification(autostart);
                    }
                    SettingSaveTimer.Start();
                    Logger.LogTrace("DisableHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Disable handler failed");
                }
            });
        }

        private void RemoveHandler(AutoStartEntry autostart)
        {
            DispatchService.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start removed: {@value}", autostart);
                    CurrentAutoStarts.Remove(autostart);
                    AllCurrentAutoStarts.Remove(autostart);
                    ResetEditablePropertiesOfAutoStarts(autostart);
                    var autostartCopy = autostart.DeepCopy();
                    autostartCopy.Date = DateTime.Now;
                    autostartCopy.Change = Change.Removed;
                    HistoryAutoStarts.Add(autostartCopy);
                    AllHistoryAutoStarts.Add(autostartCopy);
                    if (IsOwnAutoStart(autostart))
                    {
                        Logger.LogInformation("Own auto start removed");
                        AppStatus.HasOwnAutoStart = false;
                    }
                    if (NotificationsEnabled && CanAutoStartBeIgnored(autostart))
                    {
                        NotificationService.ShowRemovedAutoStartEntryNotification(autostart);
                    }
                    SettingSaveTimer.Start();
                    Logger.LogTrace("RemoveHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Remove handler failed");
                }
            });
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (SettingSaveTimer.Enabled)
                    {
                        SettingsService.Save();
                    }
                    SettingSaveTimer.Close();
                    ConnectorService.Add -= AddHandler;
                    ConnectorService.Remove -= RemoveHandler;
                    ConnectorService.Enable -= EnableHandler;
                    ConnectorService.Disable -= DisableHandler;
                    SettingsService.SettingsSaving -= SettingsSavingHandler;
                    SettingsService.SettingsLoaded -= SettingsLoadedHandler;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
