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

    public delegate void AutoStartsChangeHandler(AutoStartEntry e);

    public class AutoStartService : IDisposable {
        #region Fields
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AutoStartConnectorService Connectors = new AutoStartConnectorService();

        private readonly string PathToLastAutoStarts;

        private readonly string PathToAddedAutoStarts;

        private readonly string PathToRemovedAutoStarts;

        public Dictionary<Guid, AutoStartEntry> CurrentAutoStarts = new Dictionary<Guid, AutoStartEntry>();

        public Dictionary<Guid, AutoStartEntry> AddedAutoStarts = new Dictionary<Guid, AutoStartEntry>();

        public Dictionary<Guid, AutoStartEntry> RemovedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
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

        public bool TryGetCurrentAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.Trace("TryGetCurrentAutoStart called");
            return CurrentAutoStarts.TryGetValue(Id, out value);
        }

        public bool TryGetAddedAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.Trace("TryGetAddedAutoStart called");
            return AddedAutoStarts.TryGetValue(Id, out value);
        }

        public bool TryGetRemovedAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.Trace("TryGetRemovedAutoStart called");
            return RemovedAutoStarts.TryGetValue(Id, out value);
        }

        public void ConfirmAdd(Guid Id) {
            Logger.Trace("ConfirmAdd called");
            if (TryGetAddedAutoStart(Id, out AutoStartEntry addedAutoStart)) {
                addedAutoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                AddAutoStartChange?.Invoke(addedAutoStart);
                Logger.Info("Confirmed add of {@addedAutoStart}", addedAutoStart);
            }
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry currentAutoStart)) {
                currentAutoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                Confirm?.Invoke(currentAutoStart);
                CurrentAutoStartChange?.Invoke(currentAutoStart);
            }
        }

        public void ConfirmRemove(Guid Id) {
            Logger.Trace("ConfirmRemove called");
            if (TryGetRemovedAutoStart(Id, out AutoStartEntry autoStart)) {
                autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                RemoveAutoStartChange?.Invoke(autoStart);
                Logger.Info("Confirmed remove of {@autoStart}", autoStart);
            }
        }

        public void RemoveAutoStart(Guid Id) {
            Logger.Trace("RemoveAutoStart called");
            if (TryGetAddedAutoStart(Id, out AutoStartEntry autoStart)) {
                RemoveAutoStart(autoStart);
            }
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("RemoveAutoStart called");
            if (Connectors.CanBeDisabled(autoStart)) {
                Connectors.DisableAutoStart(autoStart);
            } else {
                Connectors.RemoveAutoStart(autoStart);
            }
            autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            Logger.Info("Removed {@autoStart}", autoStart);
        }

        public void AddAutoStart(Guid Id) {
            Logger.Trace("AddAutoStart called");
            if (TryGetRemovedAutoStart(Id, out AutoStartEntry autoStart)) {
                AddAutoStart(autoStart);
            }
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("AddAutoStart called");
            if (Connectors.CanBeEnabled(autoStart)) {
                Connectors.EnableAutoStart(autoStart);
            } else {
                Connectors.AddAutoStart(autoStart);
            }
            autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            Logger.Info("Added {@autoStart}", autoStart);
        }

        public bool CanAutoStartBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeAdded called");
            return Connectors.CanBeEnabled(autoStart) || Connectors.CanBeAdded(autoStart);
        }

        public bool CanAutoStartBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeRemoved called");
            return Connectors.CanBeDisabled(autoStart) || Connectors.CanBeRemoved(autoStart);
        }

        public bool GetIsAdminRequiredForChanges(AutoStartEntry autoStart) {
            Logger.Trace("GetIsAdminRequiredForChanges called");
            return Connectors.GetIsAdminRequiredForChanges(autoStart);
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            return Connectors.GetCurrentAutoStarts();
        }

        public bool GetAutoStartFileExists() {
            Logger.Trace("GetAutoStartFileExists called");
            return !File.Exists(PathToLastAutoStarts);
        }

        public Dictionary<Guid, AutoStartEntry> GetSavedAutoStarts(string path) {
            try {
                Logger.Trace("Loading auto starts from file {path}", path);
                if (!File.Exists(path)) {
                    return new Dictionary<Guid, AutoStartEntry>();
                }
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    IFormatter formatter = new BinaryFormatter();
                    try {
                        var ret = (Dictionary<Guid, AutoStartEntry>)formatter.Deserialize(stream);
                        Logger.Trace("Loaded last saved auto starts from file {path}", path);
                        return ret;
                    } catch (Exception ex) {
                        var err = new Exception($"Failed to deserialize from file {path}", ex);
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
                Logger.Trace("Saving current auto starts to file {path}", PathToLastAutoStarts);
                SaveAutoStarts(PathToLastAutoStarts, CurrentAutoStarts);
                Logger.Trace("Saving added auto starts to file {path}", PathToAddedAutoStarts);
                SaveAutoStarts(PathToAddedAutoStarts, AddedAutoStarts);
                Logger.Trace("Saving removed auto starts to file {path}", PathToRemovedAutoStarts);
                SaveAutoStarts(PathToRemovedAutoStarts, RemovedAutoStarts);
                Logger.Info("Saved all auto starts");
            } catch (Exception ex) {
                var err = new Exception("Failed to save current auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        private void SaveAutoStarts(string path, Dictionary<Guid, AutoStartEntry> dictionary) {
            Logger.Trace("Saving auto starts to file {path}", path);
            try {
                try {
                    var folderPath = PathToLastAutoStarts.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(folderPath);
                } catch (Exception ex) {
                    var err = new Exception($"Failed to create folder for file {path}", ex);
                    throw err;
                }
                try {
                    using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, dictionary);
                    }
                } catch (Exception ex) {
                    var err = new Exception($"Failed to write file {path}", ex);
                    throw err;
                }
                Logger.Trace("Saved auto starts to file {path}", path);
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
            try {
                Logger.Info("Comparing current auto starts to last saved");
                Dictionary<Guid, AutoStartEntry> lastSavedAutoStarts;
                try {
                    lastSavedAutoStarts = GetSavedAutoStarts(PathToLastAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load last saved auto starts", ex);
                    Logger.Error(err);
                    lastSavedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
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
                IList<AutoStartEntry> currentAutoStarts;
                try {
                    currentAutoStarts = GetCurrentAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to get current auto starts", ex);
                    throw err;
                }
                var autoStartsToRemove = new List<AutoStartEntry>();
                foreach (var lastAutostart in lastSavedAutoStarts.Values) {
                    var found = false;
                    for (int i = 0; i< currentAutoStarts.Count(); i++) {
                        var newAutostart = currentAutoStarts[i];
                        if (newAutostart.Equals(lastAutostart)) {
                            found = true;
                            currentAutoStarts[i] = lastAutostart;
                            break;
                        }
                    }
                    if (!found) {
                        autoStartsToRemove.Add(lastAutostart);
                    }
                }
                var autoStartsToAdd = new List<AutoStartEntry>();
                foreach (var newAutostart in currentAutoStarts) {
                    if (!lastSavedAutoStarts.ContainsKey(newAutostart.Id)) {
                        autoStartsToAdd.Add(newAutostart);
                    }
                }
                CurrentAutoStarts = new Dictionary<Guid, AutoStartEntry>();
                foreach (var currentAutoStart in currentAutoStarts) {
                    CurrentAutoStarts.Add(currentAutoStart.Id, currentAutoStart);
                }
                foreach (var removedAutoStart in autoStartsToRemove) {
                    RemoveHandler(removedAutoStart);
                }
                foreach (var addedAutoStart in autoStartsToAdd) {
                    AddHandler(addedAutoStart);
                }
                Logger.Trace("LoadCurrentAutoStarts finished");
            } catch (Exception ex) {
                var err = new Exception("Failed to compare current auto starts to last saved", ex);
                Logger.Error(err);
                throw err;
            }
        }

        public void StartWatcher() {
            Logger.Trace("Starting watchers");
            Connectors.StartWatcher();
        }

        public void StopWatcher() {
            Logger.Trace("Stopping watchers");
            Connectors.StopWatcher();
        }
        #endregion

        #region Events
        public event AutoStartChangeHandler Add;

        public event AutoStartChangeHandler Remove;

        public event AutoStartChangeHandler Confirm;

        public event AutoStartChangeHandler CurrentAutoStartChange;

        public event AutoStartChangeHandler AddAutoStartChange;

        public event AutoStartChangeHandler RemoveAutoStartChange;
        #endregion

        #region Event handlers
        private void AddHandler(AutoStartEntry addedAutostart) {
            try {
                Logger.Info("Auto start added: {@value}", addedAutostart);
                if (addedAutostart.AddDate == null) {
                    addedAutostart.AddDate = DateTime.Now;
                }
                if (CurrentAutoStarts.ContainsKey(addedAutostart.Id)) {
                    CurrentAutoStarts[addedAutostart.Id] = addedAutostart;
                } else {
                    CurrentAutoStarts.Add(addedAutostart.Id, addedAutostart);
                }
                if (AddedAutoStarts.ContainsKey(addedAutostart.Id)) {
                    AddedAutoStarts[addedAutostart.Id] = addedAutostart;
                } else {
                    AddedAutoStarts.Add(addedAutostart.Id, addedAutostart);
                }
                Add?.Invoke(addedAutostart);
                CurrentAutoStartChange?.Invoke(addedAutostart);
                AddAutoStartChange?.Invoke(addedAutostart); // todo: only fire on revert
                RemoveAutoStartChange?.Invoke(addedAutostart);
                Logger.Trace("AddHandler finished");
            } catch (Exception e) {
                var err = new Exception("Add handler failed", e);
                Logger.Error(err);
            }
        }

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            try {
                Logger.Info("Auto start removed: {@value}", removedAutostart);
                if (removedAutostart.RemoveDate == null) {
                    removedAutostart.RemoveDate = DateTime.Now;
                }
                // Don't directly use Dictionary.Remove() because removed auto start has different id
                foreach (var currentAutoStart in CurrentAutoStarts.Values) {
                    if (currentAutoStart.Equals(removedAutostart)) {
                        CurrentAutoStarts.Remove(currentAutoStart.Id);
                        break;
                    }
                }
                // create a new instance to prevent conflicting changes from add and remove collection
                var removedAutostartCopy = removedAutostart.DeepCopy();
                removedAutostartCopy.ConfirmStatus = ConfirmStatus.New;
                if (RemovedAutoStarts.ContainsKey(removedAutostart.Id)) {
                    RemovedAutoStarts[removedAutostart.Id] = removedAutostartCopy;
                } else {
                    RemovedAutoStarts.Add(removedAutostart.Id, removedAutostartCopy);
                }
                Remove?.Invoke(removedAutostartCopy);
                CurrentAutoStartChange?.Invoke(removedAutostart);
                AddAutoStartChange?.Invoke(removedAutostart); // todo: only fire on revert
                RemoveAutoStartChange?.Invoke(removedAutostartCopy);
                Logger.Trace("RemoveHandler finished");
            } catch (Exception e) {
                var err = new Exception("Remove handler failed", e);
                Logger.Error(err);
            }
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
