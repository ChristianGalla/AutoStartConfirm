﻿using AutoStartConfirm.Connectors;
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

    public class AutoStartService : IDisposable, IAutoStartService {
        #region Fields
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IAutoStartConnectorService connectors;
        protected IAutoStartConnectorService Connectors {
            get {
                if (connectors == null) {
                    connectors = new AutoStartConnectorService();
                    connectors.Add += AddHandler;
                    connectors.Remove += RemoveHandler;
                    connectors.Enable += EnableHandler;
                    connectors.Disable += DisableHandler;
                }
                return connectors;
            }
        }

        private readonly string PathToLastAutoStarts;

        private readonly string PathToAddedAutoStarts;

        private readonly string PathToRemovedAutoStarts;

        private Dictionary<Guid, AutoStartEntry> currentAutoStarts = new Dictionary<Guid, AutoStartEntry>();

        public Dictionary<Guid, AutoStartEntry> CurrentAutoStarts => currentAutoStarts;

        private Dictionary<Guid, AutoStartEntry> addedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
        public Dictionary<Guid, AutoStartEntry> AddedAutoStarts => addedAutoStarts;

        private Dictionary<Guid, AutoStartEntry> removedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
        public Dictionary<Guid, AutoStartEntry> RemovedAutoStarts => removedAutoStarts;
        #endregion

        #region Methods

        public AutoStartService() {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}";
            PathToLastAutoStarts = $"{basePath}LastAutoStarts.bin";
            PathToAddedAutoStarts = $"{basePath}AddedAutoStarts.bin";
            PathToRemovedAutoStarts = $"{basePath}RemovedAutoStarts.bin";
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
            if (Connectors.CanBeEnabled(autoStart)) {
                // remove disabled status to allow new entries for example at the same registry key in the future
                Connectors.EnableAutoStart(autoStart);
            }
            Connectors.RemoveAutoStart(autoStart);
            autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            Logger.Info("Removed {@autoStart}", autoStart);
        }

        public void DisableAutoStart(Guid Id) {
            Logger.Trace("DisableAutoStart called");
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry autoStart)) {
                DisableAutoStart(autoStart);
            }
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("DisableAutoStart called");
            Connectors.DisableAutoStart(autoStart);
            autoStart.ConfirmStatus = ConfirmStatus.Disabled;
            Logger.Info("Disabled {@autoStart}", autoStart);
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

        public void EnableAutoStart(Guid Id) {
            Logger.Trace("EnableAutoStart called");
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry autoStart)) {
                EnableAutoStart(autoStart);
            }
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("EnableAutoStart called");
            Connectors.EnableAutoStart(autoStart);
            autoStart.ConfirmStatus = ConfirmStatus.Enabled;
            Logger.Info("Enabled {@autoStart}", autoStart);
        }

        public bool CanAutoStartBeEnabled(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeEnabled called");
            return Connectors.CanBeEnabled(autoStart);
        }

        public bool CanAutoStartBeDisabled(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeDisabled called");
            return Connectors.CanBeDisabled(autoStart);
        }

        public bool CanAutoStartBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeAdded called");
            return Connectors.CanBeAdded(autoStart);
        }

        public bool CanAutoStartBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeRemoved called");
            return Connectors.CanBeRemoved(autoStart);
        }

        public void LoadCanBeAdded(AutoStartEntry autoStart) {
            Task.Run(() => {
                autoStart.CanBeAdded = CanAutoStartBeAdded(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
                RemoveAutoStartChange?.Invoke(autoStart);
            });
        }

        public void LoadCanBeRemoved(AutoStartEntry autoStart) {
            Task.Run(() => {
                autoStart.CanBeRemoved = CanAutoStartBeRemoved(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
                AddAutoStartChange?.Invoke(autoStart);
            });
        }

        public void LoadCanBeEnabled(AutoStartEntry autoStart) {
            Task.Run(() => {
                autoStart.CanBeEnabled = CanAutoStartBeEnabled(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
            });
        }

        public void LoadCanBeDisabled(AutoStartEntry autoStart) {
            Task.Run(() => {
                autoStart.CanBeDisabled = CanAutoStartBeDisabled(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
            });
        }

        /// <summary>
        /// Resets all dynamic properties of all known auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfAutoStarts(AutoStartEntry autoStart) {
            ResetEditablePropertiesOfCurrentAutoStarts(autoStart);
            ResetEditablePropertiesOfAddedAutoStarts(autoStart);
            ResetEditablePropertiesOfRemovedAutoStarts(autoStart);
        }

        /// <summary>
        /// Resets all dynamic properties of all current auto starts.
        /// </summary>
        public void ResetEditablePropertiesOfAllCurrentAutoStarts() {
            foreach (var autoStart in CurrentAutoStarts) {
                var autoStartValue = autoStart.Value;
                ResetAllDynamicFields(autoStartValue);
                CurrentAutoStartChange?.Invoke(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all current auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfCurrentAutoStarts(AutoStartEntry autoStart) {
            foreach (var currentAutoStart in CurrentAutoStarts) {
                var autoStartValue = currentAutoStart.Value;
                if (!autoStartValue.Path.Equals(autoStart.Path, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                ResetAllDynamicFields(autoStartValue);
                CurrentAutoStartChange?.Invoke(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all added auto starts.
        /// </summary>
        public void ResetEditablePropertiesOfAllAddedAutoStarts() {
            foreach (var autoStart in AddedAutoStarts) {
                var autoStartValue = autoStart.Value;
                ResetAllDynamicFields(autoStartValue);
                AddAutoStartChange?.Invoke(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all added auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfAddedAutoStarts(AutoStartEntry autoStart) {
            foreach (var addedAutoStart in AddedAutoStarts) {
                var autoStartValue = addedAutoStart.Value;
                if (!autoStartValue.Path.Equals(autoStart.Path, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                ResetAllDynamicFields(autoStartValue);
                AddAutoStartChange?.Invoke(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all removed auto starts.
        /// </summary>
        public void ResetEditablePropertiesOfAllRemovedAutoStarts() {
            foreach (var autoStart in RemovedAutoStarts) {
                var autoStartValue = autoStart.Value;
                ResetAllDynamicFields(autoStartValue);
                RemoveAutoStartChange?.Invoke(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all added auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfRemovedAutoStarts(AutoStartEntry autoStart) {
            foreach (var removedAutoStart in RemovedAutoStarts) {
                var autoStartValue = removedAutoStart.Value;
                if (!autoStartValue.Path.Equals(autoStart.Path, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                ResetAllDynamicFields(autoStartValue);
                RemoveAutoStartChange?.Invoke(autoStartValue);
            }
        }

        private static void ResetAllDynamicFields(AutoStartEntry autoStartValue) {
            autoStartValue.CanBeAdded = null;
            autoStartValue.CanBeRemoved = null;
            autoStartValue.CanBeEnabled = null;
            autoStartValue.CanBeDisabled = null;
        }

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            Logger.Trace("IsAdminRequiredForChanges called");
            return Connectors.IsAdminRequiredForChanges(autoStart);
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
                        Logger.Trace($"Loaded last saved auto starts from file \"{path}\"");
                        return ret;
                    } catch (Exception ex) {
                        var err = new Exception($"Failed to deserialize from file \"{path}\"", ex);
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
                    var err = new Exception($"Failed to create folder for file \"{path}\"", ex);
                    throw err;
                }
                try {
                    using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, dictionary);
                    }
                } catch (Exception ex) {
                    var err = new Exception($"Failed to write file \"{path}\"", ex);
                    throw err;
                }
                Logger.Trace($"Saved auto starts to file \"{path}\"");
            } catch (Exception ex) {
                var err = new Exception($"Failed to save auto starts to file \"{path}\"", ex);
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
                    addedAutoStarts = GetSavedAutoStarts(PathToAddedAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load added auto starts", ex);
                    Logger.Error(err);
                    addedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
                }
                try {
                    removedAutoStarts = GetSavedAutoStarts(PathToRemovedAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load removed auto starts", ex);
                    Logger.Error(err);
                    removedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
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
                    if (lastAutostart.AddDate == null) {
                        lastAutostart.AddDate = DateTime.Now;
                    }
                    for (int i = 0; i < currentAutoStarts.Count(); i++) {
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
                this.currentAutoStarts = new Dictionary<Guid, AutoStartEntry>();
                foreach (var currentAutoStart in currentAutoStarts) {
                    if (!lastSavedAutoStarts.ContainsKey(currentAutoStart.Id)) {
                        autoStartsToAdd.Add(currentAutoStart);
                    }
                    CurrentAutoStarts.Add(currentAutoStart.Id, currentAutoStart);
                }
                foreach (var currentAutoStart in CurrentAutoStarts.Values) {
                    bool wasEnabled = true;
                    if (lastSavedAutoStarts.TryGetValue(currentAutoStart.Id, out AutoStartEntry oldAutoStart)) {
                        wasEnabled = oldAutoStart.IsEnabled.GetValueOrDefault(true);
                    }
                    var nowEnabled = Connectors.IsEnabled(currentAutoStart);
                    currentAutoStart.IsEnabled = nowEnabled;
                    if (nowEnabled && !wasEnabled) {
                        EnableHandler(currentAutoStart);
                    } else if (!nowEnabled && wasEnabled) {
                        DisableHandler(currentAutoStart);
                    }
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

        public event AutoStartChangeHandler Enable;

        public event AutoStartChangeHandler Disable;

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
                ResetAllDynamicFields(addedAutostart);
                ResetEditablePropertiesOfAutoStarts(addedAutostart);
                Add?.Invoke(addedAutostart);
                CurrentAutoStartChange?.Invoke(addedAutostart);
                AddAutoStartChange?.Invoke(addedAutostart);
                Logger.Trace("AddHandler finished");
            } catch (Exception e) {
                var err = new Exception("Add handler failed", e);
                Logger.Error(err);
            }
        }

        private void EnableHandler(AutoStartEntry enabledAutostart) {
            try {
                Logger.Info("Auto start enabled: {@value}", enabledAutostart);
                foreach (var currentAutoStart in CurrentAutoStarts.Values) {
                    if (currentAutoStart.Equals(enabledAutostart)) {
                        ResetAllDynamicFields(currentAutoStart);
                        Enable?.Invoke(currentAutoStart);
                        CurrentAutoStartChange?.Invoke(currentAutoStart);
                        break;
                    }
                }
                Logger.Trace("EnableHandler finished");
            } catch (Exception e) {
                var err = new Exception("Enable handler failed", e);
                Logger.Error(err);
            }
        }

        private void DisableHandler(AutoStartEntry disabledAutostart) {
            try {
                Logger.Info("Auto start disabled: {@value}", disabledAutostart);
                foreach (var currentAutoStart in CurrentAutoStarts.Values) {
                    if (currentAutoStart.Equals(disabledAutostart)) {
                        ResetAllDynamicFields(currentAutoStart);
                        Disable?.Invoke(currentAutoStart);
                        CurrentAutoStartChange?.Invoke(currentAutoStart);
                        break;
                    }
                }
                Logger.Trace("DisableHandler finished");
            } catch (Exception e) {
                var err = new Exception("Disable handler failed", e);
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
                ResetAllDynamicFields(removedAutostartCopy);
                ResetEditablePropertiesOfAutoStarts(removedAutostartCopy);
                Remove?.Invoke(removedAutostartCopy);
                CurrentAutoStartChange?.Invoke(removedAutostart);
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
                    if (connectors != null) {
                        connectors.Dispose();
                    }
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
