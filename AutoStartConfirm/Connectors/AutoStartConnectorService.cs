using AutoStartConfirm.AutoStarts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {
    public class AutoStartConnectorService: IEnumerable<IAutoStartConnector>, IEnumerable, IDisposable, IReadOnlyCollection<IAutoStartConnector>, IReadOnlyList<IAutoStartConnector> {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected Dictionary<Category, IAutoStartConnector> Connectors = new Dictionary<Category, IAutoStartConnector>();

        public AutoStartConnectorService() {
            // todo: filter for specifiy sub sub keys if needed
            // todo: start menu links (\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup)
            // todo: User Shell Folders key (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders)
            // todo: Shell folders key (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders)
            var connectors = new List<IAutoStartConnector> {
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
            };
            foreach (var connector in connectors) {
                Connectors.Add(connector.Category, connector);
            }
            foreach (var connector in Connectors.Values) {
                connector.Add += AddHandler;
                connector.Remove += RemoveHandler;
                connector.Enable += EnableHandler;
                connector.Disable += DisableHandler;
            }
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            foreach(var connector in Connectors.Values) {
                var connectorAutoStarts = connector.GetCurrentAutoStarts();
                ret.AddRange(connectorAutoStarts);
            }
            return ret;
        }

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
            Add?.Invoke(enabledAutostart);
        }

        private void DisableHandler(AutoStartEntry disabledAutostart) {
            Logger.Trace("DisableHandler called");
            Remove?.Invoke(disabledAutostart);
        }
        #endregion

        #region IAutoStartConnector implementation
        public Category Category => throw new NotImplementedException();


        public bool CanBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be added", autoStart);
            return Connectors[autoStart.Category].CanBeAdded(autoStart);
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be removed", autoStart);
            return Connectors[autoStart.Category].CanBeRemoved(autoStart);
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Adding auto start {@autoStart}", autoStart);
            Connectors[autoStart.Category].AddAutoStart(autoStart);
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Removing auto start {@autoStart}", autoStart);
            Connectors[autoStart.Category].RemoveAutoStart(autoStart);
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be enabled", autoStart);
            return Connectors[autoStart.Category].CanBeEnabled(autoStart);
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            Logger.Trace("Checking if auto start {@autoStart} can be disabled", autoStart);
            return Connectors[autoStart.Category].CanBeDisabled(autoStart);
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Enabling auto start {@autoStart}", autoStart);
            Connectors[autoStart.Category].EnableAutoStart(autoStart);
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Disabling auto start {@autoStart}", autoStart);
            Connectors[autoStart.Category].DisableAutoStart(autoStart);
        }

        public bool GetIsAdminRequiredForChanges(AutoStartEntry autoStart) {
            return Connectors[autoStart.Category].IsAdminRequiredForChanges;
        }

        public void StartWatcher() {
            Logger.Info("Starting watchers");
            foreach (var connector in Connectors.Values) {
                connector.StartWatcher();
            }
        }

        public void StopWatcher() {
            Logger.Info("Stopping watchers");
            foreach (var connector in Connectors.Values) {
                connector.StopWatcher();
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    foreach (var connector in Connectors.Values) {
                        connector.Dispose();
                    }
                    Connectors.Clear();
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
        public int Count => ((IReadOnlyCollection<IAutoStartConnector>)Connectors).Count;

        public IAutoStartConnector this[int index] => ((IReadOnlyList<IAutoStartConnector>)Connectors)[index];

        public IEnumerator GetEnumerator() {
            return ((IEnumerable)Connectors).GetEnumerator();
        }

        IEnumerator<IAutoStartConnector> IEnumerable<IAutoStartConnector>.GetEnumerator() {
            return ((IEnumerable<IAutoStartConnector>)Connectors).GetEnumerator();
        }
        #endregion

    }
}
