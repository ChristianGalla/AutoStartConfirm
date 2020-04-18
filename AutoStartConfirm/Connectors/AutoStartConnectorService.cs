using AutoStartConfirm.AutoStarts;
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
            Connectors.Add(Category.BootExecute, new BootExecuteConnector());
            Connectors.Add(Category.AppInit32, new AppInit32Connector());
            Connectors.Add(Category.AppInit64, new AppInit64Connector());
            Connectors.Add(Category.AppCertDll, new AppCertDllConnector());
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
