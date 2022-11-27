using AutoStartConfirm.Connectors.Folder;
using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.Connectors.ScheduledTask;
using AutoStartConfirm.Connectors.Services;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace AutoStartConfirm.Connectors
{
    public class AutoStartConnectorService : IEnumerable<IAutoStartConnector>, IEnumerable, IDisposable, IReadOnlyCollection<IAutoStartConnector>, IReadOnlyList<IAutoStartConnector>, IAutoStartConnectorService {

        #region Attributes

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Dictionary<Category, IAutoStartConnector> allConnectors;
        public Dictionary<Category, IAutoStartConnector> AllConnectors {
            get {
                if (allConnectors == null) {
                    CreateConnectors();
                }
                return allConnectors;
            }
        }

        private Dictionary<Category, IAutoStartConnector> enabledConnectors;

        public Dictionary<Category, IAutoStartConnector> EnabledConnectors {
            get {
                if (enabledConnectors == null) {
                    CreateOrUpdateEnabledConnectors();
                }
                return enabledConnectors;
            }
        }

        private App app;

        public App App {
            get {
                if (app == null) {
                    app = (App)Application.Current;
                }
                return app;
            }
            set {
                app = value;
            }
        }

        public bool WatcherStarted { get; private set; }

        private ISettingsService settingsService;

        public ISettingsService SettingsService {
            get {
                if (settingsService == null) {
                    settingsService = App.SettingsService;
                }
                return settingsService;
            }
            set => settingsService = value;
        }
        #endregion

        #region Methods

        public AutoStartConnectorService() {
            SettingsService.SettingsSaving += SettingsSavingHandler;
            SettingsService.SettingsLoaded += SettingsLoadedHandler;
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

        private void CreateConnectors() {
            // todo: filter for specifiy sub sub keys if needed
            // todo: User Shell Folders key (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders)
            // todo: Shell folders key (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders)
            var connectorList = new List<IAutoStartConnector> {
                new BootExecuteConnector(),
                new AppInit32Connector(),
                new AppInit64Connector(),
                new AppCertDllConnector(),
                new LogonConnector(),
                new UserInitMprLogonScriptConnector(),
                new GroupPolicyExtensionsConnector(),
                new DomainGroupPolicyScriptStartupConnector(),
                new DomainGroupPolicyScriptShutdownConnector(),
                new DomainGroupPolicyScriptLogonConnector(),
                new DomainGroupPolicyScriptLogoffConnector(),
                new LocalGroupPolicyScriptStartupConnector(),
                new LocalGroupPolicyScriptShutdownConnector(),
                new LocalGroupPolicyScriptLogonConnector(),
                new LocalGroupPolicyScriptLogoffConnector(),
                new GroupPolicyShellOverwriteConnector(),
                new AlternateShellConnector(),
                new AvailableShellsConnector(),
                new TerminalServerStartupProgramsConnector(),
                new TerminalServerRunConnector(),
                new TerminalServerRunOnceConnector(),
                new TerminalServerRunOnceExConnector(),
                new TerminalServerInitialProgramConnector(),
                new Run32Connector(),
                new RunOnce32Connector(),
                new RunOnceEx32Connector(),
                new Run64Connector(),
                new RunOnce64Connector(),
                new RunOnceEx64Connector(),
                new GroupPolicyRunConnector(),
                new ActiveSetup32Connector(),
                new ActiveSetup64Connector(),
                new IconServiceLibConnector(),
                new WindowsCEServicesAutoStartOnConnect32Connector(),
                new WindowsCEServicesAutoStartOnDisconnect32Connector(),
                new WindowsCEServicesAutoStartOnConnect64Connector(),
                new WindowsCEServicesAutoStartOnDisconnect64Connector(),
                new CurrentUserLocalGroupPolicyScriptStartupConnector(),
                new CurrentUserLocalGroupPolicyScriptShutdownConnector(),
                new CurrentUserLocalGroupPolicyScriptLogonConnector(),
                new CurrentUserLocalGroupPolicyScriptLogoffConnector(),
                new CurrentUserUserInitMprLogonScriptConnector(),
                new CurrentUserGroupPolicyShellOverwriteConnector(),
                new CurrentUserLoadConnector(),
                new CurrentUserGroupPolicyRunConnector(),
                new CurrentUserRun32Connector(),
                new CurrentUserRunOnce32Connector(),
                new CurrentUserRunOnceEx32Connector(),
                new CurrentUserRun64Connector(),
                new CurrentUserRunOnce64Connector(),
                new CurrentUserRunOnceEx64Connector(),
                new CurrentUserTerminalServerRunConnector(),
                new CurrentUserTerminalServerRunOnceConnector(),
                new CurrentUserTerminalServerRunOnceExConnector(),
                new StartMenuAutoStartFolderConnector(),
                new CurrentUserStartMenuAutoStartFolderConnector(),
                new ScheduledTaskConnector(),
                new DeviceServiceConnector(),
                new OtherServiceConnector(),
            };
            allConnectors = new Dictionary<Category, IAutoStartConnector>();
            foreach (var connector in connectorList) {
                allConnectors.Add(connector.Category, connector);
            }
            foreach (var connector in allConnectors.Values) {
                connector.Add += AddHandler;
                connector.Remove += RemoveHandler;
                connector.Enable += EnableHandler;
                connector.Disable += DisableHandler;
            }
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
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
            Logger.Trace("AddHandler called");
            Add?.Invoke(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            Logger.Trace("RemoveHandler called");
            Remove?.Invoke(removedAutostart);
        }

        private void EnableHandler(AutoStartEntry enabledAutostart) {
            Logger.Trace("EnableHandler called");
            Enable?.Invoke(enabledAutostart);
        }

        private void DisableHandler(AutoStartEntry disabledAutostart) {
            Logger.Trace("DisableHandler called");
            Disable?.Invoke(disabledAutostart);
        }

        private void SettingsLoadedHandler(object sender, System.Configuration.SettingsLoadedEventArgs e) {
            CreateOrUpdateEnabledConnectors();
        }

        private void SettingsSavingHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            CreateOrUpdateEnabledConnectors();
        }

        #endregion

        #region IAutoStartConnector implementation
        public Category Category => throw new NotImplementedException();


        public bool CanBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be added", autoStart);
            return AllConnectors[autoStart.Category].CanBeAdded(autoStart);
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be removed", autoStart);
            return AllConnectors[autoStart.Category].CanBeRemoved(autoStart);
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Adding auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].AddAutoStart(autoStart);
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Removing auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].RemoveAutoStart(autoStart);
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be enabled", autoStart);
            return AllConnectors[autoStart.Category].CanBeEnabled(autoStart);
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be disabled", autoStart);
            return AllConnectors[autoStart.Category].CanBeDisabled(autoStart);
        }

        public bool IsEnabled(AutoStartEntry autoStart) {
            return AllConnectors[autoStart.Category].IsEnabled(autoStart);
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Enabling auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].EnableAutoStart(autoStart);
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Disabling auto start {@autoStart}", autoStart);
            AllConnectors[autoStart.Category].DisableAutoStart(autoStart);
        }

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            return AllConnectors[autoStart.Category].IsAdminRequiredForChanges(autoStart);
        }

        public void StartWatcher() {
            Logger.Info("Starting watchers");
            WatcherStarted = true;
            foreach (var connector in EnabledConnectors.Values) {
                try {
                    connector.StartWatcher();
                } catch (NotImplementedException) {
                }
            }
        }

        public void StopWatcher() {
            Logger.Info("Stopping watchers");
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
