using AutoStartConfirm.Connectors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        private readonly string PathToHistoryAutoStarts;

        private Dictionary<Guid, AutoStartEntry> currentAutoStarts = new Dictionary<Guid, AutoStartEntry>();

        public Dictionary<Guid, AutoStartEntry> CurrentAutoStarts => currentAutoStarts;

        private ObservableCollection<AutoStartEntry> historyAutoStarts = new ObservableCollection<AutoStartEntry>();

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts => historyAutoStarts;
        #endregion

        #region Methods

        public AutoStartService() {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}";
            PathToLastAutoStarts = $"{basePath}LastAutoStarts.bin";
            PathToHistoryAutoStarts = $"{basePath}HistoryAutoStarts.bin";
        }

        public bool TryGetCurrentAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.Trace("TryGetCurrentAutoStart called");
            return CurrentAutoStarts.TryGetValue(Id, out value);
        }

        public bool TryGetHistoryAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.Trace("TryGetHistoryAutoStart called");
            value = null;
            foreach (var autoStart in HistoryAutoStarts) {
                if (autoStart.Id == Id) {
                    value = autoStart;
                    return true;
                }
            }
            return false;
        }


        public void ConfirmAdd(Guid Id) {
            Logger.Trace("ConfirmAdd called");
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry addedAutoStart)) {
                addedAutoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                HistoryAutoStartChange?.Invoke(addedAutoStart);
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
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry autoStart)) {
                autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                HistoryAutoStartChange?.Invoke(autoStart);
                Logger.Info("Confirmed remove of {@autoStart}", autoStart);
            }
        }

        public void RemoveAutoStart(Guid Id) {
            Logger.Trace("RemoveAutoStart called");
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry autoStart)) {
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
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry autoStart)) {
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
                HistoryAutoStartChange?.Invoke(autoStart);
            });
        }

        public void LoadCanBeRemoved(AutoStartEntry autoStart) {
            Task.Run(() => {
                autoStart.CanBeRemoved = CanAutoStartBeRemoved(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
                HistoryAutoStartChange?.Invoke(autoStart);
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
            ResetEditablePropertiesOfHistoryAutoStarts(autoStart);
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
        /// Resets all dynamic properties of all history auto starts.
        /// </summary>
        public void ResetEditablePropertiesOfAllHistoryAutoStarts() {
            foreach (var autoStart in HistoryAutoStarts) {
                ResetAllDynamicFields(autoStart);
                HistoryAutoStartChange?.Invoke(autoStart);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all history auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfHistoryAutoStarts(AutoStartEntry autoStart) {
            foreach (var historyAutoStart in HistoryAutoStarts) {
                if (!historyAutoStart.Path.Equals(autoStart.Path, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                ResetAllDynamicFields(historyAutoStart);
                HistoryAutoStartChange?.Invoke(historyAutoStart);
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

        public Dictionary<Guid, AutoStartEntry> GetSavedCurrentAutoStarts(string path) {
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

        public ObservableCollection<AutoStartEntry> GetSavedHistoryAutoStarts(string path) {
            try {
                Logger.Trace("Loading auto starts from file {path}", path);
                if (!File.Exists(path)) {
                    return new ObservableCollection<AutoStartEntry>();
                }
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    IFormatter formatter = new BinaryFormatter();
                    try {
                        var ret = (ObservableCollection<AutoStartEntry>)formatter.Deserialize(stream);
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
                Logger.Trace("Saving history auto starts to file {path}", PathToHistoryAutoStarts);
                SaveAutoStarts(PathToHistoryAutoStarts, HistoryAutoStarts);
                Logger.Info("Saved all auto starts");
            } catch (Exception ex) {
                var err = new Exception("Failed to save current auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        private void SaveAutoStarts(string path, object dictionary) {
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
                    lastSavedAutoStarts = GetSavedCurrentAutoStarts(PathToLastAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load last saved auto starts", ex);
                    Logger.Error(err);
                    lastSavedAutoStarts = new Dictionary<Guid, AutoStartEntry>();
                }
                try {
                    historyAutoStarts = GetSavedHistoryAutoStarts(PathToHistoryAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load removed auto starts", ex);
                    Logger.Error(err);
                    historyAutoStarts = new ObservableCollection<AutoStartEntry>();
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
                    if (lastAutostart.Date == null) {
                        lastAutostart.Date = DateTime.Now;
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

        public event AutoStartChangeHandler HistoryAutoStartChange;
        #endregion

        #region Event handlers
        private void AddHandler(AutoStartEntry autostart) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    Logger.Info("Auto start added: {@value}", autostart);
                    autostart.Date = DateTime.Now;
                    autostart.Change = Change.Added;
                    ResetEditablePropertiesOfAutoStarts(autostart);
                    CurrentAutoStarts.Add(autostart.Id, autostart);
                    HistoryAutoStarts.Add(autostart);
                    Add?.Invoke(autostart);
                    CurrentAutoStartChange?.Invoke(autostart);
                    HistoryAutoStartChange?.Invoke(autostart);
                    Logger.Trace("AddHandler finished");
                } catch (Exception e) {
                    var err = new Exception("Add handler failed", e);
                    Logger.Error(err);
                }
            });
        }

        private void EnableHandler(AutoStartEntry autostart) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    Logger.Info("Auto start enabled: {@value}", autostart);
                    ResetAllDynamicFields(autostart);
                    var autostartCopy = autostart.DeepCopy();
                    autostartCopy.Date = DateTime.Now;
                    autostartCopy.Change = Change.Enabled;
                    CurrentAutoStarts[autostart.Id] = autostartCopy;
                    HistoryAutoStarts.Add(autostartCopy);
                    Enable?.Invoke(autostartCopy);
                    CurrentAutoStartChange?.Invoke(autostartCopy);
                    HistoryAutoStartChange?.Invoke(autostartCopy);
                    Logger.Trace("EnableHandler finished");
                } catch (Exception e) {
                    var err = new Exception("Enable handler failed", e);
                    Logger.Error(err);
                }
            });
        }

        private void DisableHandler(AutoStartEntry autostart) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    Logger.Info("Auto start disabled: {@value}", autostart);
                    ResetAllDynamicFields(autostart);
                    var autostartCopy = autostart.DeepCopy();
                    autostartCopy.Date = DateTime.Now;
                    autostartCopy.Change = Change.Disabled;
                    CurrentAutoStarts[autostart.Id] = autostartCopy;
                    HistoryAutoStarts.Add(autostartCopy);
                    Disable?.Invoke(autostartCopy);
                    CurrentAutoStartChange?.Invoke(autostartCopy);
                    HistoryAutoStartChange?.Invoke(autostartCopy);
                    Logger.Trace("DisableHandler finished");
                } catch (Exception e) {
                    var err = new Exception("Disable handler failed", e);
                    Logger.Error(err);
                }
            });
        }

        private void RemoveHandler(AutoStartEntry autostart) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    Logger.Info("Auto start removed: {@value}", autostart);
                    // Don't directly use Dictionary.Remove() because removed auto start has different id
                    foreach (var currentAutoStart in CurrentAutoStarts.Values) {
                        if (currentAutoStart.Equals(autostart)) {
                            CurrentAutoStarts.Remove(currentAutoStart.Id);
                            break;
                        }
                    }
                    ResetAllDynamicFields(autostart);
                    var autostartCopy = autostart.DeepCopy();
                    autostartCopy.Date = DateTime.Now;
                    autostartCopy.Change = Change.Removed;
                    HistoryAutoStarts.Add(autostartCopy);
                    Remove?.Invoke(autostart);
                    CurrentAutoStartChange?.Invoke(autostart);
                    HistoryAutoStartChange?.Invoke(autostartCopy);
                    Logger.Trace("RemoveHandler finished");
                } catch (Exception e) {
                    var err = new Exception("Remove handler failed", e);
                    Logger.Error(err);
                }
            });
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
