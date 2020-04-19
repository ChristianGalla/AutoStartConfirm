﻿using AutoStartConfirm.AutoStarts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {
    public class AutoStartConnectorService: IEnumerable<IAutoStartConnector>, IEnumerable, IDisposable, IAutoStartConnector, IReadOnlyCollection<IAutoStartConnector>, IReadOnlyList<IAutoStartConnector> {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected Dictionary<Category, IAutoStartConnector> Connectors = new Dictionary<Category, IAutoStartConnector>();

        public AutoStartConnectorService() {
            // todo: filter for specifiy sub sub keys if needed
            // todo: \ProgramData\Microsoft\Windows\Start Menu\Programs\Startup
            var connectors = new List<IAutoStartConnector> {
                new BootExecuteConnector(),
                new AppInit32Connector(),
                new AppInit64Connector(),
                new AppCertDllConnector(),
                new LogonConnector(),
                new UserInitMprLogonScriptConnector(),
                new GroupPolicyExtensionsConnector(),
                new DomainGroupPolicyScriptConnector(),
                new LocalGroupPolicyScriptConnector(),
                new GroupPolicyShellOverwriteConnector(),
                new AlternateShellConnector(),
                new AvailableShellsConnector(),
                new TerminalServerStartupProgramsConnector(),
                new TerminalServerRunConnector(),
                new TerminalServerInitialProgramConnector(),
                new Run32Connector(),
                new Run64Connector(),
                new GroupPolicyRunConnector(),
                new ActiveSetup32Connector(),
                new ActiveSetup64Connector(),
                new IconServiceLibConnector(),
                new WindowsCEServices32Connector(),
                new WindowsCEServices64Connector(),
            };
            foreach (var connector in connectors) {
                try {
                    Connectors.Add(connector.Category, connector);
                } catch (Exception ex) {
                    throw new Exception($"Failed to add {connector.GetType()}", ex);
                }
            }
            foreach (var connector in Connectors.Values) {
                connector.Add += AddHandler;
                connector.Remove += RemoveHandler;
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
        #endregion

        #region IAutoStartConnector implementation
        public Category Category => throw new NotImplementedException();

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Adding auto start {@autoStart}", autoStart);
            Connectors[autoStart.Category].AddAutoStart(autoStart);
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.Info("Removing auto start {@autoStart}", autoStart);
            Connectors[autoStart.Category].RemoveAutoStart(autoStart);
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
