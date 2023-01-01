using AutoStartConfirm.Connectors.Folder;
using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.Connectors.ScheduledTask;
using AutoStartConfirm.Connectors.Services;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;

namespace AutoStartConfirm.Connectors
{
    public class AutoStartConnectorService : IEnumerable<IAutoStartConnector>, IEnumerable, IDisposable, IReadOnlyCollection<IAutoStartConnector>, IReadOnlyList<IAutoStartConnector>, IAutoStartConnectorService {

        #region Attributes

        private readonly ILogger<AutoStartConnectorService> Logger;

        private Dictionary<Category, IAutoStartConnector> AllConnectors = new Dictionary<Category, IAutoStartConnector>();

        private Dictionary<Category, IAutoStartConnector> enabledConnectors;

        public Dictionary<Category, IAutoStartConnector> EnabledConnectors {
            get {
                if (enabledConnectors == null) {
                    CreateOrUpdateEnabledConnectors();
                }
                return enabledConnectors;
            }
        }

        public bool WatcherStarted { get; private set; }

        private readonly ISettingsService SettingsService;
        #endregion

        #region Methods

        public AutoStartConnectorService(
            ILogger<AutoStartConnectorService> logger,
            ISettingsService settingsService,
            IBootExecuteConnector bootExecuteConnector,
            IAppInit32Connector appInit32Connector,
            IAppInit64Connector appInit64Connector,
            IAppCertDllConnector appCertDllConnector,
            ILogonConnector logonConnector,
            IUserInitMprLogonScriptConnector userInitMprLogonScriptConnector,
            IGroupPolicyExtensionsConnector groupPolicyExtensionsConnector,
            IDomainGroupPolicyScriptStartupConnector domainGroupPolicyScriptStartupConnector,
            IDomainGroupPolicyScriptShutdownConnector domainGroupPolicyScriptShutdownConnector,
            IDomainGroupPolicyScriptLogonConnector domainGroupPolicyScriptLogonConnector,
            IDomainGroupPolicyScriptLogoffConnector domainGroupPolicyScriptLogoffConnector,
            ILocalGroupPolicyScriptStartupConnector localGroupPolicyScriptStartupConnector,
            ILocalGroupPolicyScriptShutdownConnector localGroupPolicyScriptShutdownConnector,
            ILocalGroupPolicyScriptLogonConnector localGroupPolicyScriptLogonConnector,
            ILocalGroupPolicyScriptLogoffConnector localGroupPolicyScriptLogoffConnector,
            IGroupPolicyShellOverwriteConnector groupPolicyShellOverwriteConnector,
            IAlternateShellConnector alternateShellConnector,
            IAvailableShellsConnector availableShellsConnector,
            ITerminalServerStartupProgramsConnector terminalServerStartupProgramsConnector,
            ITerminalServerRunConnector terminalServerRunConnector,
            ITerminalServerRunOnceConnector terminalServerRunOnceConnector,
            ITerminalServerRunOnceExConnector terminalServerRunOnceExConnector,
            ITerminalServerInitialProgramConnector terminalServerInitialProgramConnector,
            IRun32Connector run32Connector,
            IRunOnce32Connector runOnce32Connector,
            IRunOnceEx32Connector runOnceEx32Connector,
            IRun64Connector run64Connector,
            IRunOnce64Connector runOnce64Connector,
            IRunOnceEx64Connector runOnceEx64Connector,
            IGroupPolicyRunConnector groupPolicyRunConnector,
            IActiveSetup32Connector activeSetup32Connector,
            IActiveSetup64Connector activeSetup64Connector,
            IIconServiceLibConnector iconServiceLibConnector,
            IWindowsCEServicesAutoStartOnConnect32Connector windowsCEServicesAutoStartOnConnect32Connector,
            IWindowsCEServicesAutoStartOnDisconnect32Connector windowsCEServicesAutoStartOnDisconnect32Connector,
            IWindowsCEServicesAutoStartOnConnect64Connector windowsCEServicesAutoStartOnConnect64Connector,
            IWindowsCEServicesAutoStartOnDisconnect64Connector windowsCEServicesAutoStartOnDisconnect64Connector,
            ICurrentUserLocalGroupPolicyScriptStartupConnector currentUserLocalGroupPolicyScriptStartupConnector,
            ICurrentUserLocalGroupPolicyScriptShutdownConnector currentUserLocalGroupPolicyScriptShutdownConnector,
            ICurrentUserLocalGroupPolicyScriptLogonConnector currentUserLocalGroupPolicyScriptLogonConnector,
            ICurrentUserLocalGroupPolicyScriptLogoffConnector currentUserLocalGroupPolicyScriptLogoffConnector,
            ICurrentUserUserInitMprLogonScriptConnector currentUserUserInitMprLogonScriptConnector,
            ICurrentUserGroupPolicyShellOverwriteConnector currentUserGroupPolicyShellOverwriteConnector,
            ICurrentUserLoadConnector currentUserLoadConnector,
            ICurrentUserGroupPolicyRunConnector currentUserGroupPolicyRunConnector,
            ICurrentUserRun32Connector currentUserRun32Connector,
            ICurrentUserRunOnce32Connector currentUserRunOnce32Connector,
            ICurrentUserRunOnceEx32Connector currentUserRunOnceEx32Connector,
            ICurrentUserRun64Connector currentUserRun64Connector,
            ICurrentUserRunOnce64Connector currentUserRunOnce64Connector,
            ICurrentUserRunOnceEx64Connector currentUserRunOnceEx64Connector,
            ICurrentUserTerminalServerRunConnector currentUserTerminalServerRunConnector,
            ICurrentUserTerminalServerRunOnceConnector currentUserTerminalServerRunOnceConnector,
            ICurrentUserTerminalServerRunOnceExConnector currentUserTerminalServerRunOnceExConnector,
            IStartMenuAutoStartFolderConnector startMenuAutoStartFolderConnector,
            ICurrentUserStartMenuAutoStartFolderConnector currentUserStartMenuAutoStartFolderConnector,
            IScheduledTaskConnector scheduledTaskConnector,
            IDeviceServiceConnector deviceServiceConnector,
            IOtherServiceConnector otherServiceConnector
        ) {
            Logger = logger;
            SettingsService = settingsService;
            SettingsService.SettingsSaving += SettingsSavingHandler;
            SettingsService.SettingsLoaded += SettingsLoadedHandler;



            // todo: filter for specifiy sub sub keys if needed
            // todo: User Shell Folders key (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders)
            // todo: Shell folders key (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders)
            AllConnectors.Add(bootExecuteConnector.Category, bootExecuteConnector);
            AllConnectors.Add(appInit32Connector.Category, appInit32Connector);
            AllConnectors.Add(appInit64Connector.Category, appInit64Connector);
            AllConnectors.Add(appCertDllConnector.Category, appCertDllConnector);
            AllConnectors.Add(logonConnector.Category, logonConnector);
            AllConnectors.Add(userInitMprLogonScriptConnector.Category, userInitMprLogonScriptConnector);
            AllConnectors.Add(groupPolicyExtensionsConnector.Category, groupPolicyExtensionsConnector);
            AllConnectors.Add(domainGroupPolicyScriptStartupConnector.Category, domainGroupPolicyScriptStartupConnector);
            AllConnectors.Add(domainGroupPolicyScriptShutdownConnector.Category, domainGroupPolicyScriptShutdownConnector);
            AllConnectors.Add(domainGroupPolicyScriptLogonConnector.Category, domainGroupPolicyScriptLogonConnector);
            AllConnectors.Add(domainGroupPolicyScriptLogoffConnector.Category, domainGroupPolicyScriptLogoffConnector);
            AllConnectors.Add(localGroupPolicyScriptStartupConnector.Category, localGroupPolicyScriptStartupConnector);
            AllConnectors.Add(localGroupPolicyScriptShutdownConnector.Category, localGroupPolicyScriptShutdownConnector);
            AllConnectors.Add(localGroupPolicyScriptLogonConnector.Category, localGroupPolicyScriptLogonConnector);
            AllConnectors.Add(localGroupPolicyScriptLogoffConnector.Category, localGroupPolicyScriptLogoffConnector);
            AllConnectors.Add(groupPolicyShellOverwriteConnector.Category, groupPolicyShellOverwriteConnector);
            AllConnectors.Add(alternateShellConnector.Category, alternateShellConnector);
            AllConnectors.Add(availableShellsConnector.Category, availableShellsConnector);
            AllConnectors.Add(terminalServerStartupProgramsConnector.Category, terminalServerStartupProgramsConnector);
            AllConnectors.Add(terminalServerRunConnector.Category, terminalServerRunConnector);
            AllConnectors.Add(terminalServerRunOnceConnector.Category, terminalServerRunOnceConnector);
            AllConnectors.Add(terminalServerRunOnceExConnector.Category, terminalServerRunOnceExConnector);
            AllConnectors.Add(terminalServerInitialProgramConnector.Category, terminalServerInitialProgramConnector);
            AllConnectors.Add(run32Connector.Category, run32Connector);
            AllConnectors.Add(runOnce32Connector.Category, runOnce32Connector);
            AllConnectors.Add(runOnceEx32Connector.Category, runOnceEx32Connector);
            AllConnectors.Add(run64Connector.Category, run64Connector);
            AllConnectors.Add(runOnce64Connector.Category, runOnce64Connector);
            AllConnectors.Add(runOnceEx64Connector.Category, runOnceEx64Connector);
            AllConnectors.Add(groupPolicyRunConnector.Category, groupPolicyRunConnector);
            AllConnectors.Add(activeSetup32Connector.Category, activeSetup32Connector);
            AllConnectors.Add(activeSetup64Connector.Category, activeSetup64Connector);
            AllConnectors.Add(iconServiceLibConnector.Category, iconServiceLibConnector);
            AllConnectors.Add(windowsCEServicesAutoStartOnConnect32Connector.Category, windowsCEServicesAutoStartOnConnect32Connector);
            AllConnectors.Add(windowsCEServicesAutoStartOnDisconnect32Connector.Category, windowsCEServicesAutoStartOnDisconnect32Connector);
            AllConnectors.Add(windowsCEServicesAutoStartOnConnect64Connector.Category, windowsCEServicesAutoStartOnConnect64Connector);
            AllConnectors.Add(windowsCEServicesAutoStartOnDisconnect64Connector.Category, windowsCEServicesAutoStartOnDisconnect64Connector);
            AllConnectors.Add(currentUserLocalGroupPolicyScriptStartupConnector.Category, currentUserLocalGroupPolicyScriptStartupConnector);
            AllConnectors.Add(currentUserLocalGroupPolicyScriptShutdownConnector.Category, currentUserLocalGroupPolicyScriptShutdownConnector);
            AllConnectors.Add(currentUserLocalGroupPolicyScriptLogonConnector.Category, currentUserLocalGroupPolicyScriptLogonConnector);
            AllConnectors.Add(currentUserLocalGroupPolicyScriptLogoffConnector.Category, currentUserLocalGroupPolicyScriptLogoffConnector);
            AllConnectors.Add(currentUserUserInitMprLogonScriptConnector.Category, currentUserUserInitMprLogonScriptConnector);
            AllConnectors.Add(currentUserGroupPolicyShellOverwriteConnector.Category, currentUserGroupPolicyShellOverwriteConnector);
            AllConnectors.Add(currentUserLoadConnector.Category, currentUserLoadConnector);
            AllConnectors.Add(currentUserGroupPolicyRunConnector.Category, currentUserGroupPolicyRunConnector);
            AllConnectors.Add(currentUserRun32Connector.Category, currentUserRun32Connector);
            AllConnectors.Add(currentUserRunOnce32Connector.Category, currentUserRunOnce32Connector);
            AllConnectors.Add(currentUserRunOnceEx32Connector.Category, currentUserRunOnceEx32Connector);
            AllConnectors.Add(currentUserRun64Connector.Category, currentUserRun64Connector);
            AllConnectors.Add(currentUserRunOnce64Connector.Category, currentUserRunOnce64Connector);
            AllConnectors.Add(currentUserRunOnceEx64Connector.Category, currentUserRunOnceEx64Connector);
            AllConnectors.Add(currentUserTerminalServerRunConnector.Category, currentUserTerminalServerRunConnector);
            AllConnectors.Add(currentUserTerminalServerRunOnceConnector.Category, currentUserTerminalServerRunOnceConnector);
            AllConnectors.Add(currentUserTerminalServerRunOnceExConnector.Category, currentUserTerminalServerRunOnceExConnector);
            AllConnectors.Add(startMenuAutoStartFolderConnector.Category, startMenuAutoStartFolderConnector);
            AllConnectors.Add(currentUserStartMenuAutoStartFolderConnector.Category, currentUserStartMenuAutoStartFolderConnector);
            AllConnectors.Add(scheduledTaskConnector.Category, scheduledTaskConnector);
            AllConnectors.Add(deviceServiceConnector.Category, deviceServiceConnector);
            AllConnectors.Add(otherServiceConnector.Category, otherServiceConnector);
            foreach (var connector in AllConnectors.Values)
            {
                connector.Add += AddHandler;
                connector.Remove += RemoveHandler;
                connector.Enable += EnableHandler;
                connector.Disable += DisableHandler;
            }
        }

        private void CreateOrUpdateEnabledConnectors() {
            var newEnabledConnectors = new Dictionary<Category, IAutoStartConnector>();
            foreach (var connector in AllConnectors.Values) {
                var isEnabled = !SettingsService.DisabledConnectors.Contains(connector.Category.ToString());
                if (isEnabled) {
                    newEnabledConnectors.Add(connector.Category, connector);
                }
                if (isEnabled && WatcherStarted) {
                    connector.StartWatcher();
                } else {
                    connector.StopWatcher();
                }
            }
            enabledConnectors = newEnabledConnectors;
        }

        public static void ConfigureServices(ServiceCollection services)
        {
            services
                .AddSingleton<IAutoStartConnectorService, AutoStartConnectorService>()
                .AddTransient<IRegistryDisableService, RegistryDisableService>()
                .AddTransient<IRegistryChangeMonitor, RegistryChangeMonitor>()
                .AddTransient<IScheduledTaskConnector, ScheduledTaskConnector>()
                .AddTransient<IFolderChangeMonitor, FolderChangeMonitor>()
                .AddSingleton<IBootExecuteConnector, BootExecuteConnector>()
                .AddSingleton<IAppInit32Connector, AppInit32Connector>()
                .AddSingleton<IAppInit64Connector, AppInit64Connector>()
                .AddSingleton<IAppCertDllConnector, AppCertDllConnector>()
                .AddSingleton<ILogonConnector, LogonConnector>()
                .AddSingleton<IUserInitMprLogonScriptConnector, UserInitMprLogonScriptConnector>()
                .AddSingleton<IGroupPolicyExtensionsConnector, GroupPolicyExtensionsConnector>()
                .AddSingleton<IDomainGroupPolicyScriptStartupConnector, DomainGroupPolicyScriptStartupConnector>()
                .AddSingleton<IDomainGroupPolicyScriptShutdownConnector, DomainGroupPolicyScriptShutdownConnector>()
                .AddSingleton<IDomainGroupPolicyScriptLogonConnector, DomainGroupPolicyScriptLogonConnector>()
                .AddSingleton<IDomainGroupPolicyScriptLogoffConnector, DomainGroupPolicyScriptLogoffConnector>()
                .AddSingleton<ILocalGroupPolicyScriptStartupConnector, LocalGroupPolicyScriptStartupConnector>()
                .AddSingleton<ILocalGroupPolicyScriptShutdownConnector, LocalGroupPolicyScriptShutdownConnector>()
                .AddSingleton<ILocalGroupPolicyScriptLogonConnector, LocalGroupPolicyScriptLogonConnector>()
                .AddSingleton<ILocalGroupPolicyScriptLogoffConnector, LocalGroupPolicyScriptLogoffConnector>()
                .AddSingleton<IGroupPolicyShellOverwriteConnector, GroupPolicyShellOverwriteConnector>()
                .AddSingleton<IAlternateShellConnector, AlternateShellConnector>()
                .AddSingleton<IAvailableShellsConnector, AvailableShellsConnector>()
                .AddSingleton<ITerminalServerStartupProgramsConnector, TerminalServerStartupProgramsConnector>()
                .AddSingleton<ITerminalServerRunConnector, TerminalServerRunConnector>()
                .AddSingleton<ITerminalServerRunOnceConnector, TerminalServerRunOnceConnector>()
                .AddSingleton<ITerminalServerRunOnceExConnector, TerminalServerRunOnceExConnector>()
                .AddSingleton<ITerminalServerInitialProgramConnector, TerminalServerInitialProgramConnector>()
                .AddSingleton<IRun32Connector, Run32Connector>()
                .AddSingleton<IRunOnce32Connector, RunOnce32Connector>()
                .AddSingleton<IRunOnceEx32Connector, RunOnceEx32Connector>()
                .AddSingleton<IRun64Connector, Run64Connector>()
                .AddSingleton<IRunOnce64Connector, RunOnce64Connector>()
                .AddSingleton<IRunOnceEx64Connector, RunOnceEx64Connector>()
                .AddSingleton<IGroupPolicyRunConnector, GroupPolicyRunConnector>()
                .AddSingleton<IActiveSetup32Connector, ActiveSetup32Connector>()
                .AddSingleton<IActiveSetup64Connector, ActiveSetup64Connector>()
                .AddSingleton<IIconServiceLibConnector, IconServiceLibConnector>()
                .AddSingleton<IWindowsCEServicesAutoStartOnConnect32Connector, WindowsCEServicesAutoStartOnConnect32Connector>()
                .AddSingleton<IWindowsCEServicesAutoStartOnDisconnect32Connector, WindowsCEServicesAutoStartOnDisconnect32Connector>()
                .AddSingleton<IWindowsCEServicesAutoStartOnConnect64Connector, WindowsCEServicesAutoStartOnConnect64Connector>()
                .AddSingleton<IWindowsCEServicesAutoStartOnDisconnect64Connector, WindowsCEServicesAutoStartOnDisconnect64Connector>()
                .AddSingleton<ICurrentUserLocalGroupPolicyScriptStartupConnector, CurrentUserLocalGroupPolicyScriptStartupConnector>()
                .AddSingleton<ICurrentUserLocalGroupPolicyScriptShutdownConnector, CurrentUserLocalGroupPolicyScriptShutdownConnector>()
                .AddSingleton<ICurrentUserLocalGroupPolicyScriptLogonConnector, CurrentUserLocalGroupPolicyScriptLogonConnector>()
                .AddSingleton<ICurrentUserLocalGroupPolicyScriptLogoffConnector, CurrentUserLocalGroupPolicyScriptLogoffConnector>()
                .AddSingleton<ICurrentUserUserInitMprLogonScriptConnector, CurrentUserUserInitMprLogonScriptConnector>()
                .AddSingleton<ICurrentUserGroupPolicyShellOverwriteConnector, CurrentUserGroupPolicyShellOverwriteConnector>()
                .AddSingleton<ICurrentUserLoadConnector, CurrentUserLoadConnector>()
                .AddSingleton<ICurrentUserGroupPolicyRunConnector, CurrentUserGroupPolicyRunConnector>()
                .AddSingleton<ICurrentUserRun32Connector, CurrentUserRun32Connector>()
                .AddSingleton<ICurrentUserRunOnce32Connector, CurrentUserRunOnce32Connector>()
                .AddSingleton<ICurrentUserRunOnceEx32Connector, CurrentUserRunOnceEx32Connector>()
                .AddSingleton<ICurrentUserRun64Connector, CurrentUserRun64Connector>()
                .AddSingleton<ICurrentUserRunOnce64Connector, CurrentUserRunOnce64Connector>()
                .AddSingleton<ICurrentUserRunOnceEx64Connector, CurrentUserRunOnceEx64Connector>()
                .AddSingleton<ICurrentUserTerminalServerRunConnector, CurrentUserTerminalServerRunConnector>()
                .AddSingleton<ICurrentUserTerminalServerRunOnceConnector, CurrentUserTerminalServerRunOnceConnector>()
                .AddSingleton<ICurrentUserTerminalServerRunOnceExConnector, CurrentUserTerminalServerRunOnceExConnector>()
                .AddSingleton<IStartMenuAutoStartFolderConnector, StartMenuAutoStartFolderConnector>()
                .AddSingleton<ICurrentUserStartMenuAutoStartFolderConnector, CurrentUserStartMenuAutoStartFolderConnector>()
                .AddSingleton<IDeviceServiceConnector, DeviceServiceConnector>()
                .AddSingleton<IOtherServiceConnector, OtherServiceConnector>();
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.LogTrace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            foreach (var connector in EnabledConnectors.Values) {
                var connectorAutoStarts = connector.GetCurrentAutoStarts();
                ret.AddRange(connectorAutoStarts);
            }
            return ret;
        }

        #endregion

        #region Events
        public event AutoStartChangeHandler Add;

        public event AutoStartChangeHandler Remove;

        public event AutoStartChangeHandler Enable;

        public event AutoStartChangeHandler Disable;
        #endregion

        #region Event handlers
        private void AddHandler(AutoStartEntry addedAutostart) {
            Logger.LogTrace("AddHandler called");
            Add?.Invoke(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            Logger.LogTrace("RemoveHandler called");
            Remove?.Invoke(removedAutostart);
        }

        private void EnableHandler(AutoStartEntry enabledAutostart) {
            Logger.LogTrace("EnableHandler called");
            Enable?.Invoke(enabledAutostart);
        }

        private void DisableHandler(AutoStartEntry disabledAutostart) {
            Logger.LogTrace("DisableHandler called");
            Disable?.Invoke(disabledAutostart);
        }

        private void SettingsLoadedHandler(object sender, SettingsLoadedEventArgs e) {
            CreateOrUpdateEnabledConnectors();
        }

        private void SettingsSavingHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            CreateOrUpdateEnabledConnectors();
        }

        #endregion

        #region IAutoStartConnector implementation

#pragma warning disable CA1065
        public Category Category => throw new NotImplementedException();
#pragma warning restore CA1065

        public bool CanBeAdded(AutoStartEntry autoStart) {
            Logger.LogTrace("Checking if auto start {@autoStart} can be added", autoStart);
            return AllConnectors[autoStart.Category].CanBeAdded(autoStart);
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            Logger.LogTrace("Checking if auto start {@autoStart} can be removed", autoStart);
            return AllConnectors[autoStart.Category].CanBeRemoved(autoStart);
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.LogInformation("Adding auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].AddAutoStart(autoStart);
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.LogInformation("Removing auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].RemoveAutoStart(autoStart);
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            Logger.LogTrace("Checking if auto start {@autoStart} can be enabled", autoStart);
            return AllConnectors[autoStart.Category].CanBeEnabled(autoStart);
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            Logger.LogTrace("Checking if auto start {@autoStart} can be disabled", autoStart);
            return AllConnectors[autoStart.Category].CanBeDisabled(autoStart);
        }

        public bool IsEnabled(AutoStartEntry autoStart) {
            return AllConnectors[autoStart.Category].IsEnabled(autoStart);
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            Logger.LogInformation("Enabling auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].EnableAutoStart(autoStart);
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            Logger.LogInformation("Disabling auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].DisableAutoStart(autoStart);
        }

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            return AllConnectors[autoStart.Category].IsAdminRequiredForChanges(autoStart);
        }

        public void StartWatcher() {
            Logger.LogInformation("Starting watchers");
            WatcherStarted = true;
            foreach (var connector in EnabledConnectors.Values) {
                try {
                    connector.StartWatcher();
                } catch (NotImplementedException) {
                }
            }
        }

        public void StopWatcher() {
            Logger.LogInformation("Stopping watchers");
            WatcherStarted = false;
            foreach (var connector in EnabledConnectors.Values) {
                try {
                    connector.StopWatcher();
                } catch (NotImplementedException) {
                }
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    SettingsService.SettingsSaving -= SettingsSavingHandler;
                    SettingsService.SettingsLoaded -= SettingsLoadedHandler;
                    foreach (var connector in AllConnectors.Values) {
                        connector.Add -= AddHandler;
                        connector.Remove -= RemoveHandler;
                        connector.Enable -= EnableHandler;
                        connector.Disable -= DisableHandler;
                        connector.Dispose();
                    }
                    EnabledConnectors.Clear();
                    AllConnectors.Clear();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

        #region Enumerator implementations
        public int Count => ((IReadOnlyCollection<IAutoStartConnector>)EnabledConnectors).Count;

        public IAutoStartConnector this[int index] => ((IReadOnlyList<IAutoStartConnector>)EnabledConnectors)[index];

        public IEnumerator GetEnumerator() {
            return ((IEnumerable)EnabledConnectors).GetEnumerator();
        }

        IEnumerator<IAutoStartConnector> IEnumerable<IAutoStartConnector>.GetEnumerator() {
            return ((IEnumerable<IAutoStartConnector>)EnabledConnectors).GetEnumerator();
        }
        #endregion

    }
}
