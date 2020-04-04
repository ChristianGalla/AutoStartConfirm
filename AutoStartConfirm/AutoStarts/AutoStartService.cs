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

        private readonly string PathToAddedAutoStarts;

        private readonly string PathToRemovedAutoStarts;

        private Dictionary<Guid, AutoStartEntry> CurrentAutoStarts = null;

        private Dictionary<Guid, AutoStartEntry> AddedAutoStarts = null;

        private Dictionary<Guid, AutoStartEntry> RemovedAutoStarts = null;
        #endregion

        #region Methods

        public AutoStartService() {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}";
            PathToLastAutoStarts = $"{basePath}LastAutoStarts.bin";
            PathToAddedAutoStarts = $"{basePath}AddedAutoStarts.bin";
            PathToRemovedAutoStarts = $"{basePath}RemovedAutoStarts.bin";
            Connectors.Add += AddHandler;
            Connectors.Remove += RemoveHandler;
        }

        public AutoStartEntry GetCurrentAutoStart(Guid Id) {
            if (CurrentAutoStarts.TryGetValue(Id, out AutoStartEntry ret)) {
                return ret;
            }
            return null;
        }

        public AutoStartEntry GetAddedAutoStart(Guid Id) {
            if (AddedAutoStarts.TryGetValue(Id, out AutoStartEntry ret)) {
                return ret;
            }
            return null;
        }

        public AutoStartEntry GetRemovedAutoStart(Guid Id) {
            if (RemovedAutoStarts.TryGetValue(Id, out AutoStartEntry ret)) {
                return ret;
            }
            return null;
        }

        public AutoStartEntry GetAutoStart(Category category, string path, string name) {
            return null;
        }

        public void ConfirmAdd(Guid Id) {
            var autoStart = GetAddedAutoStart(Id);
            if (autoStart != null) {
                autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            }
            autoStart = GetCurrentAutoStart(Id);
            if (autoStart != null) {
                autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            }
        }

        public void ConfirmRemove(Guid Id) {
            var autoStart = GetRemovedAutoStart(Id);
            if (autoStart != null) {
                autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            }
        }

        public void RevertAdd(Guid Id) {
            var autoStart = GetAddedAutoStart(Id);
            if (autoStart != null && autoStart.ConfirmStatus != ConfirmStatus.Reverted) {
                // todo: revert auto start
                autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            }
        }

        public void RevertRemove(Guid Id) {
            var autoStart = GetRemovedAutoStart(Id);
            if (autoStart != null && autoStart.ConfirmStatus != ConfirmStatus.Reverted) {
                // todo: revert auto start
                autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            }
        }

        public IEnumerable<AutoStartEntry> GetCurrentAutoStarts() {
            return Connectors.GetCurrentAutoStarts();
        }

        public Dictionary<Guid, AutoStartEntry> GetSavedAutoStarts(string path) {
            Logger.Info("Loading auto starts from file");
            try {
                if (!File.Exists(path)) {
                    return new Dictionary<Guid, AutoStartEntry>();
                }
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
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
            try {
                Logger.Info("Saving current known auto starts");
                SaveAutoStarts(PathToLastAutoStarts, CurrentAutoStarts);
                Logger.Debug("Saved current auto starts");
                SaveAutoStarts(PathToAddedAutoStarts, AddedAutoStarts);
                Logger.Debug("Saved added auto starts");
                SaveAutoStarts(PathToRemovedAutoStarts, RemovedAutoStarts);
                Logger.Debug("Saved removed auto starts");
                Logger.Info("Saved all auto starts");
            } catch (Exception ex) {
                var err = new Exception("Failed to save current auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        private void SaveAutoStarts(string path, Dictionary<Guid, AutoStartEntry> dictionary) {
            try {
                try {
                    var folderPath = PathToLastAutoStarts.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(folderPath);
                } catch (Exception ex) {
                    var err = new Exception("Failed to create folder", ex);
                    throw err;
                }
                try {
                    using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, CurrentAutoStarts);
                    }
                } catch (Exception ex) {
                    var err = new Exception("Failed to write file", ex);
                    throw err;
                }
            } catch (Exception ex) {
                var err = new Exception($"Failed to save auto starts to file {path}", ex);
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
                    CurrentAutoStarts = GetSavedAutoStarts(PathToLastAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load last auto starts", ex);
                    Logger.Error(err);
                    CurrentAutoStarts = new Dictionary<Guid, AutoStartEntry>();
                }
                try {
                    AddedAutoStarts = GetSavedAutoStarts(PathToAddedAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load added auto starts", ex);
                    Logger.Error(err);
                    AddedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
                }
                try {
                    RemovedAutoStarts = GetSavedAutoStarts(PathToRemovedAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load removed auto starts", ex);
                    Logger.Error(err);
                    RemovedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
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
            if (addedAutostart.AddDate == null) {
                addedAutostart.AddDate = DateTime.Now;
            }
            CurrentAutoStarts.Add(addedAutostart.Id, addedAutostart);
            AddedAutoStarts.Add(addedAutostart.Id, addedAutostart);
            Add?.Invoke(addedAutostart);
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            if (removedAutostart.RemoveDate == null) {
                removedAutostart.RemoveDate = DateTime.Now;
            }
            CurrentAutoStarts.Remove(removedAutostart.Id);
            RemovedAutoStarts.Add(removedAutostart.Id, removedAutostart);
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
