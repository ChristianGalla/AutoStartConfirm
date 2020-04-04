using AutoStartConfirm.Connectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.AutoStarts {
    class AutoStartService : IDisposable {
        #region Fields
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AutoStartConnectorService Connectors = new AutoStartConnectorService();
        private readonly string PathToLastAutoStarts;

        private Dictionary<Guid, AutoStartEntry> CurrentAutoStarts = null;
        #endregion

        #region Methods

        public AutoStartService() {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            PathToLastAutoStarts = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}LastAutoStarts.bin";
            Connectors.Add += AddHandler;
            Connectors.Remove += RemoveHandler;
        }

        public AutoStartEntry GetAutoStart(Guid Id) {
            return null;
        }

        public AutoStartEntry GetAutoStart(Category category, string path, string name) {
            return null;
        }

        public void ConfirmAutoStart(Guid Id) {
            var autoStart = GetAutoStart(Id);
            if (autoStart != null) {
                autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            }
        }

        public void RevertAutoStart(Guid Id) {
            var autoStart = GetAutoStart(Id);
            if (autoStart != null && autoStart.ConfirmStatus != ConfirmStatus.Reverted) {
                // todo: revert auto start
                autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            }
        }

        public IEnumerable<AutoStartEntry> GetCurrentAutoStarts() {
            return Connectors.GetCurrentAutoStarts();
        }

        public Dictionary<Guid, AutoStartEntry> GetSavedAutoStarts() {
            Logger.Info("Loading last auto starts");
            try {
                if (!File.Exists(PathToLastAutoStarts)) {
                    return new Dictionary<Guid, AutoStartEntry>();
                }
                using (Stream stream = new FileStream(PathToLastAutoStarts, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    IFormatter formatter = new BinaryFormatter();
                    try {
                        var ret = (Dictionary<Guid, AutoStartEntry>)formatter.Deserialize(stream);
                        Logger.Info("Deserialized last saved auto starts");
                        return ret;
                    } catch (Exception ex) {
                        var err = new Exception("Failed to deserialize", ex);
                        throw err;
                    }
                }
            } catch (Exception ex) {
                var err = new Exception("Failed to load last auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        public void SaveAutoStarts() {
            Logger.Info("Saving current known auto starts");
            try {
                try {
                    var folderPath = PathToLastAutoStarts.Substring(0, PathToLastAutoStarts.LastIndexOf(Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(folderPath);
                } catch (Exception ex) {
                    var err = new Exception("Failed to create folder", ex);
                    throw err;
                }
                try {
                    using (Stream stream = new FileStream(PathToLastAutoStarts, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, CurrentAutoStarts);
                    }
                } catch (Exception ex) {
                    var err = new Exception("Failed to write file", ex);
                    throw err;
                }
                Logger.Info("Saved current auto starts");
            } catch (Exception ex) {
                var err = new Exception("Failed to save current auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        /// <summary>
        /// Loads current autostarts, compares to last saved and fires add or remove events if necessary
        /// </summary>
        public void LoadCurrentAutoStarts() {
            Logger.Info("Comparing current auto starts to last saved");
            try {
                try {
                    CurrentAutoStarts = GetSavedAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to load last auto starts", ex);
                    Logger.Error(err);
                    CurrentAutoStarts = new Dictionary<Guid, AutoStartEntry>();
                }
                IEnumerable<AutoStartEntry> currentAutoStarts;
                try {
                    currentAutoStarts = GetCurrentAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to get current auto starts", ex);
                    throw err;
                }
                var lastAutostarts = CurrentAutoStarts.Values.ToList();
                foreach (var lastAutostart in lastAutostarts) {
                    var found = false;
                    foreach (var newAutostart in currentAutoStarts) {
                        if (newAutostart.Category == lastAutostart.Category && newAutostart.Path == lastAutostart.Path && newAutostart.Name == lastAutostart.Name) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        RemoveHandler(lastAutostart);
                    }
                }
                foreach (var newAutostart in currentAutoStarts) {
                    var found = false;
                    foreach (var lastAutostart in lastAutostarts) {
                        if (newAutostart.Category == lastAutostart.Category && newAutostart.Path == lastAutostart.Path && newAutostart.Name == lastAutostart.Name) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        AddHandler(newAutostart);
                    }
                }
            } catch (Exception ex) {
                var err = new Exception("Failed to compare current auto starts to last saved", ex);
                Logger.Error(err);
                throw err;
            }
        }

        public void StartWatcher() {
            Connectors.StartWatcher();
        }

        public void StopWatcher() {
            Connectors.StopWatcher();
        }
        #endregion

        #region Events
        public event AddHandler Add;

        public event RemoveHandler Remove;
        #endregion

        #region Event handlers
        private void AddHandler(AutoStartEntry addedAutostart) {
            CurrentAutoStarts.Add(addedAutostart.Id, addedAutostart);
            Add?.Invoke(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            CurrentAutoStarts.Remove(removedAutostart.Id);
            Remove?.Invoke(removedAutostart);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Connectors.Dispose();
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
    }
}
