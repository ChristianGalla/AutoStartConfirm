using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.Exceptions;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoStartConfirm.Connectors {

    public delegate void AutoStartsChangeHandler(AutoStartEntry e);

    public class AutoStartService : IDisposable, IAutoStartService {
        #region Fields
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IAutoStartConnectorService connectorServce;
        public IAutoStartConnectorService ConnectorService {
            get {
                if (connectorServce == null) {
                    connectorServce = new AutoStartConnectorService();
                    connectorServce.Add += AddHandler;
                    connectorServce.Remove += RemoveHandler;
                    connectorServce.Enable += EnableHandler;
                    connectorServce.Disable += DisableHandler;
                }
                return connectorServce;
            }
            set {
                connectorServce = value;
            }
        }

        private string currentExePath;

        public string CurrentExePath {
            get {
                if (currentExePath == null) {
                    currentExePath = Assembly.GetEntryAssembly().Location;
                }
                return currentExePath;
            }
            set {
                currentExePath = value;
            }
        }

        private readonly string PathToLastAutoStarts;

        private readonly string PathToHistoryAutoStarts;


        private ObservableCollection<AutoStartEntry> currentAutoStarts = new ObservableCollection<AutoStartEntry>();


        public ObservableCollection<AutoStartEntry> CurrentAutoStarts => currentAutoStarts;

        private ObservableCollection<AutoStartEntry> allCurrentAutoStarts = new ObservableCollection<AutoStartEntry>();

        public ObservableCollection<AutoStartEntry> AllCurrentAutoStarts => allCurrentAutoStarts;


        private ObservableCollection<AutoStartEntry> historyAutoStarts = new ObservableCollection<AutoStartEntry>();

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts => historyAutoStarts;


        private ObservableCollection<AutoStartEntry> allHistoryAutoStarts = new ObservableCollection<AutoStartEntry>();

        public ObservableCollection<AutoStartEntry> AllHistoryAutoStarts => allHistoryAutoStarts;

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


        private CurrentUserRun64Connector currentUserRun64Connector;

        public CurrentUserRun64Connector CurrentUserRun64Connector {
            get {
                if (currentUserRun64Connector == null) {
                    currentUserRun64Connector = new CurrentUserRun64Connector();
                }
                return currentUserRun64Connector;
            }
            set => currentUserRun64Connector = value;
        }

        public bool HasOwnAutoStart {
            get {
                var currentAutoStarts = CurrentUserRun64Connector.GetCurrentAutoStarts();
                foreach (var autoStart in currentAutoStarts) {
                    if (IsOwnAutoStart(autoStart)) {
                        return true;
                    }
                }
                return false;
            }
        }
        #endregion

        #region Methods

        public AutoStartService() {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}";
            PathToLastAutoStarts = $"{basePath}LastAutoStarts.bin";
            PathToHistoryAutoStarts = $"{basePath}HistoryAutoStarts.bin";
            SettingsService.SettingsSaving += SettingsSavingHandler;
            SettingsService.SettingsLoaded += SettingsLoadedHandler;
        }

        private void SettingsLoadedHandler(object sender, SettingsLoadedEventArgs e) {
            HandleSettingChanges();
        }

        private void SettingsSavingHandler(object sender, CancelEventArgs e) {
            HandleSettingChanges();
        }

        private void HandleSettingChanges() {
            Application.Current.Dispatcher.Invoke(delegate {
                CurrentAutoStarts.Clear();
                foreach (var autoStart in AllCurrentAutoStarts) {
                    if (!SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString())) {
                        CurrentAutoStarts.Add(autoStart);
                    }
                }
                HistoryAutoStarts.Clear();
                foreach (var autoStart in AllHistoryAutoStarts) {
                    if (!SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString())) {
                        HistoryAutoStarts.Add(autoStart);
                    }
                }
            });
        }

        public bool TryGetCurrentAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.Trace("TryGetCurrentAutoStart called");
            value = null;
            foreach (var autoStart in AllCurrentAutoStarts) {
                if (autoStart.Id == Id) {
                    value = autoStart;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetHistoryAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.Trace("TryGetHistoryAutoStart called");
            value = null;
            foreach (var autoStart in AllHistoryAutoStarts) {
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

        public void ConfirmAdd(AutoStartEntry autoStart) {
            autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            ConfirmAdd(autoStart.Id);
        }

        public void ConfirmRemove(AutoStartEntry autoStart) {
            autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
            ConfirmRemove(autoStart.Id);
        }

        public void RemoveAutoStart(Guid Id) {
            Logger.Trace("RemoveAutoStart called");
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry autoStart)) {
                RemoveAutoStart(autoStart);
            }
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("RemoveAutoStart called");
            if (ConnectorService.CanBeEnabled(autoStart)) {
                // remove disabled status to allow new entries for example at the same registry key in the future
                ConnectorService.EnableAutoStart(autoStart);
            }
            ConnectorService.RemoveAutoStart(autoStart);
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
            ConnectorService.DisableAutoStart(autoStart);
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
            ConnectorService.AddAutoStart(autoStart);
            try {
                ConnectorService.EnableAutoStart(autoStart);
            } catch (AlreadySetException) {

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
            ConnectorService.EnableAutoStart(autoStart);
            autoStart.ConfirmStatus = ConfirmStatus.Enabled;
            Logger.Info("Enabled {@autoStart}", autoStart);
        }

        public bool CanAutoStartBeEnabled(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeEnabled called");
            return ConnectorService.CanBeEnabled(autoStart);
        }

        public bool CanAutoStartBeDisabled(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeDisabled called");
            return ConnectorService.CanBeDisabled(autoStart);
        }

        public bool CanAutoStartBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeAdded called");
            return ConnectorService.CanBeAdded(autoStart);
        }

        public bool CanAutoStartBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("CanAutoStartBeRemoved called");
            return ConnectorService.CanBeRemoved(autoStart);
        }

        public async Task LoadCanBeAdded(AutoStartEntry autoStart) {
            await Task.Run(() => {
                autoStart.CanBeAdded = CanAutoStartBeAdded(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
                HistoryAutoStartChange?.Invoke(autoStart);
            });
        }

        public async Task LoadCanBeRemoved(AutoStartEntry autoStart) {
            await Task.Run(() => {
                autoStart.CanBeRemoved = CanAutoStartBeRemoved(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
                HistoryAutoStartChange?.Invoke(autoStart);
            });
        }

        public async Task LoadCanBeEnabled(AutoStartEntry autoStart) {
            await Task.Run(() => {
                autoStart.CanBeEnabled = CanAutoStartBeEnabled(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
                HistoryAutoStartChange?.Invoke(autoStart);
            });
        }

        public async Task LoadCanBeDisabled(AutoStartEntry autoStart) {
            await Task.Run(() => {
                autoStart.CanBeDisabled = CanAutoStartBeDisabled(autoStart);
                CurrentAutoStartChange?.Invoke(autoStart);
                HistoryAutoStartChange?.Invoke(autoStart);
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
                var autoStartValue = autoStart;
                ResetAllDynamicFields(autoStartValue);
                CurrentAutoStartChange?.Invoke(autoStartValue);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all current auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfCurrentAutoStarts(AutoStartEntry autoStart) {
            foreach (var currentAutoStart in AllCurrentAutoStarts) {
                var autoStartValue = currentAutoStart;
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
            foreach (var autoStart in AllHistoryAutoStarts) {
                ResetAllDynamicFields(autoStart);
                HistoryAutoStartChange?.Invoke(autoStart);
            }
        }

        /// <summary>
        /// Resets all dynamic properties of all history auto starts at the same path as the given one.
        /// </summary>
        /// <param name="autoStart"></param>
        public void ResetEditablePropertiesOfHistoryAutoStarts(AutoStartEntry autoStart) {
            foreach (var historyAutoStart in AllHistoryAutoStarts) {
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
            return ConnectorService.IsAdminRequiredForChanges(autoStart);
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            return ConnectorService.GetCurrentAutoStarts();
        }

        public bool GetValidAutoStartFileExists() {
            Logger.Trace("GetAutoStartFileExists called");
            if (!File.Exists(PathToLastAutoStarts)) {
                return false;
            }
            try {
                GetSavedCurrentAutoStarts(PathToLastAutoStarts);
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public ObservableCollection<AutoStartEntry> GetSavedCurrentAutoStarts(string path) {
            try {
                Logger.Trace("Loading auto starts from file {path}", path);
                if (!File.Exists(path)) {
                    return new ObservableCollection<AutoStartEntry>();
                }
                using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    IFormatter formatter = new BinaryFormatter();
                    try {
                        var ret = (ObservableCollection<AutoStartEntry>)formatter.Deserialize(stream);
                        var autoStartsToRemove = new List<AutoStartEntry>();
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
                SaveAutoStarts(PathToLastAutoStarts, AllCurrentAutoStarts);
                Logger.Trace("Saving history auto starts to file {path}", PathToHistoryAutoStarts);
                SaveAutoStarts(PathToHistoryAutoStarts, AllHistoryAutoStarts);
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

                // get last saved auto starts
                ObservableCollection<AutoStartEntry> lastSavedAutoStarts;
                try {
                    lastSavedAutoStarts = GetSavedCurrentAutoStarts(PathToLastAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load last saved auto starts", ex);
                    Logger.Error(err);
                    lastSavedAutoStarts = new ObservableCollection<AutoStartEntry>();
                }
                AllCurrentAutoStarts.Clear();
                var lastSavedAutoStartsDictionary = new Dictionary<Guid, AutoStartEntry>();
                foreach(var lastSavedAutoStart in lastSavedAutoStarts) {
                    if (lastSavedAutoStart.Date == null) {
                        lastSavedAutoStart.Date = DateTime.Now;
                    }
                    lastSavedAutoStartsDictionary.Add(lastSavedAutoStart.Id, lastSavedAutoStart);
                    if (SettingsService.DisabledConnectors.Contains(lastSavedAutoStart.Category.ToString())) {
                        AllCurrentAutoStarts.Add(lastSavedAutoStart);
                    }
                }

                // get history auto starts
                try {
                    allHistoryAutoStarts = GetSavedHistoryAutoStarts(PathToHistoryAutoStarts);
                } catch (Exception ex) {
                    var err = new Exception("Failed to load removed auto starts", ex);
                    Logger.Error(err);
                    allHistoryAutoStarts = new ObservableCollection<AutoStartEntry>();
                }
                HistoryAutoStarts.Clear();
                foreach (var lastAutostart in allHistoryAutoStarts) {
                    if (!SettingsService.DisabledConnectors.Contains(lastAutostart.Category.ToString())) {
                        HistoryAutoStarts.Add(lastAutostart);
                    }
                }

                // get current auto starts
                IList<AutoStartEntry> currentAutoStarts;
                try {
                    currentAutoStarts = GetCurrentAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to get current auto starts", ex);
                    throw err;
                }
                var currentAutoStartDictionary = new Dictionary<Guid, AutoStartEntry>();
                foreach (var currentAutoStart in currentAutoStarts) {
                    foreach (var lastAutoStart in lastSavedAutoStarts) {
                        if (currentAutoStart.Equals(lastAutoStart)) {
                            currentAutoStart.Id = lastAutoStart.Id;
                            break;
                        }
                    }
                    currentAutoStartDictionary.Add(currentAutoStart.Id, currentAutoStart);
                }

                // get auto starts to remove
                var autoStartsToRemove = new List<AutoStartEntry>();
                foreach (var lastAutostart in lastSavedAutoStarts) {
                    if (!currentAutoStartDictionary.ContainsKey(lastAutostart.Id) &&
                        !SettingsService.DisabledConnectors.Contains(lastAutostart.Category.ToString())) {
                        autoStartsToRemove.Add(lastAutostart);
                    }
                }

                // get auto starts to add
                var autoStartsToAdd = new List<AutoStartEntry>();
                CurrentAutoStarts.Clear();
                foreach (var currentAutoStart in currentAutoStarts) {
                    if (lastSavedAutoStartsDictionary.TryGetValue(currentAutoStart.Id, out AutoStartEntry lastAutoStartEntry)) {
                        CurrentAutoStarts.Add(lastAutoStartEntry);
                        AllCurrentAutoStarts.Add(lastAutoStartEntry);
                    } else {
                        // add handler will add auto start later to CurrentAutoStarts collection
                        autoStartsToAdd.Add(currentAutoStart);
                    }
                }

                // call remove handlers
                foreach (var removedAutoStart in autoStartsToRemove) {
                    RemoveHandler(removedAutoStart);
                }

                // call add handlers
                foreach (var addedAutoStart in autoStartsToAdd) {
                    AddHandler(addedAutoStart);
                }

                // call enable / disable handlers
                foreach (var currentAutoStart in currentAutoStarts) {
                    bool wasEnabled = true;
                    if (lastSavedAutoStartsDictionary.TryGetValue(currentAutoStart.Id, out AutoStartEntry oldAutoStart)) {
                        wasEnabled = oldAutoStart.IsEnabled.GetValueOrDefault(true);
                    }
                    var nowEnabled = ConnectorService.IsEnabled(currentAutoStart);
                    currentAutoStart.IsEnabled = nowEnabled;
                    if (nowEnabled && !wasEnabled) {
                        EnableHandler(currentAutoStart);
                    } else if (!nowEnabled && wasEnabled) {
                        DisableHandler(currentAutoStart);
                    }
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
            ConnectorService.StartWatcher();
        }

        public void StopWatcher() {
            Logger.Trace("Stopping watchers");
            ConnectorService.StopWatcher();
        }

        public bool IsOwnAutoStart(AutoStartEntry autoStart) {
            return autoStart.Category == Category.CurrentUserRun64 &&
            autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
            autoStart.Value == CurrentExePath;
        }


        public void ToggleOwnAutoStart() {
            try {
                Logger.Info("ToggleOwnAutoStart called");
                var ownAutoStart = new RegistryAutoStartEntry() {
                    Category = Category.CurrentUserRun64,
                    Path = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm",
                    Value = CurrentExePath,
                    RegistryValueKind = Microsoft.Win32.RegistryValueKind.String,
                    ConfirmStatus = ConfirmStatus.New,
                };

                if (HasOwnAutoStart) {
                    Logger.Info("Shall remove own auto start");
                    RemoveAutoStart(ownAutoStart);
                } else {
                    Logger.Info("Shall add own auto start");
                    AddAutoStart(ownAutoStart);
                }
                ownAutoStart.ConfirmStatus = ConfirmStatus.New;
                Logger.Trace("Own auto start toggled");
            } catch (Exception e) {
                var message = "Failed to toggle own auto start";
                var err = new Exception(message, e);
                Logger.Error(err);
            }
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
                    CurrentAutoStarts.Add(autostart);
                    AllCurrentAutoStarts.Add(autostart);
                    HistoryAutoStarts.Add(autostart);
                    AllHistoryAutoStarts.Add(autostart);
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
                    CurrentAutoStarts.Remove(autostart);
                    CurrentAutoStarts.Add(autostartCopy);
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
                    CurrentAutoStarts.Remove(autostart);
                    CurrentAutoStarts.Add(autostartCopy);
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
                    CurrentAutoStarts.Remove(autostart);
                    AllCurrentAutoStarts.Remove(autostart);
                    ResetAllDynamicFields(autostart);
                    var autostartCopy = autostart.DeepCopy();
                    autostartCopy.Date = DateTime.Now;
                    autostartCopy.Change = Change.Removed;
                    HistoryAutoStarts.Add(autostartCopy);
                    AllHistoryAutoStarts.Add(autostartCopy);
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
                    if (connectorServce != null) {
                        connectorServce.Add -= AddHandler;
                        connectorServce.Remove -= RemoveHandler;
                        connectorServce.Enable -= EnableHandler;
                        connectorServce.Disable -= DisableHandler;
                        connectorServce.Dispose();
                        SettingsService.SettingsSaving -= SettingsSavingHandler;
                        SettingsService.SettingsLoaded -= SettingsLoadedHandler;
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
