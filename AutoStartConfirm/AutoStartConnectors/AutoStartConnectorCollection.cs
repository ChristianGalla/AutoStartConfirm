using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.AutoStartConnectors {
    class AutoStartConnectorCollection: IEnumerable<IAutoStartConnector>, IEnumerable, IDisposable, IAutoStartConnector, IReadOnlyCollection<IAutoStartConnector>, IReadOnlyList<IAutoStartConnector> {

        protected List<IAutoStartConnector> Connectors = new List<IAutoStartConnector>();

        public AutoStartConnectorCollection() {
            Connectors.Add(new BootExecuteConnector());
        }

        public IEnumerable<AutoStartEntry> GetCurrentAutoStarts() {
            var ret = new List<AutoStartEntry>();
            foreach(var connector in Connectors) {
                var connectorAutoStarts = connector.GetCurrentAutoStarts();
                ret.AddRange(connectorAutoStarts);
            }
            return ret;
        }

        #region IAutoStartConnector implementation
        public event AddHandler Add;
        public event RemoveHandler Remove;
        public void StartWatcher() {
            foreach (var connector in Connectors) {
                connector.StartWatcher();
            }
        }

        public void StopWatcher() {
            foreach (var connector in Connectors) {
                connector.StopWatcher();
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    foreach (var connector in Connectors) {
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
