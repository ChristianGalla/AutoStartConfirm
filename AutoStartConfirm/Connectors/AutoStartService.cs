using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.Exceptions;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace AutoStartConfirm.Connectors
{

    public delegate void AutoStartsChangeHandler(AutoStartEntry e);

    public class AutoStartService : IDisposable, IAutoStartService {
        #region Fields
        private readonly ILogger<AutoStartService> Logger;

        private readonly IAutoStartConnectorService ConnectorService;

        private string currentExePath;

        public string CurrentExePath {
            get {
                currentExePath ??= Environment.ProcessPath;
                return currentExePath;
            }
            set {
                currentExePath = value;
            }
        }

        private readonly string PathToLastAutoStarts;

        private readonly string PathToHistoryAutoStarts;


        private readonly ObservableCollection<AutoStartEntry> currentAutoStarts = new();

        public ObservableCollection<AutoStartEntry> CurrentAutoStarts => currentAutoStarts;


        private readonly ObservableCollection<AutoStartEntry> allCurrentAutoStarts = new();

        public ObservableCollection<AutoStartEntry> AllCurrentAutoStarts => allCurrentAutoStarts;


        private readonly ObservableCollection<AutoStartEntry> historyAutoStarts = new();

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts => historyAutoStarts;


        private ObservableCollection<AutoStartEntry> allHistoryAutoStarts = new();

        public ObservableCollection<AutoStartEntry> AllHistoryAutoStarts => allHistoryAutoStarts;

        private readonly ISettingsService SettingsService;

        private readonly ICurrentUserRun64Connector CurrentUserRun64Connector;
        private readonly IDispatchService DispatchService;

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

        public AutoStartService(
            ILogger<AutoStartService> logger,
            IAutoStartConnectorService connectorService,
            ISettingsService settingsService,
            ICurrentUserRun64Connector currentUserRun64Connector,
            IDispatchService dispatchService
        ) {
            Logger = logger;
            ConnectorService = connectorService;
            SettingsService = settingsService;
            CurrentUserRun64Connector = currentUserRun64Connector;
            DispatchService = dispatchService;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var basePath = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}";
            ConnectorService.Add += AddHandler;
            ConnectorService.Remove += RemoveHandler;
            ConnectorService.Enable += EnableHandler;
            ConnectorService.Disable += DisableHandler;
            PathToLastAutoStarts = $"{basePath}LastAutoStarts";
            PathToHistoryAutoStarts = $"{basePath}HistoryAutoStarts";
            SettingsService.SettingsSaving += SettingsSavingHandler;
            SettingsService.SettingsLoaded += SettingsLoadedHandler;
        }

        private void SettingsLoadedHandler(object sender, SettingsLoadedEventArgs e) {
            HandleSettingChanges();
        }

        private void SettingsSavingHandler(object sender, CancelEventArgs e) {
            HandleSettingChanges();
        }

        private void HandleSettingChanges()
        {
            CurrentAutoStarts.Clear();
            foreach (var autoStart in AllCurrentAutoStarts)
            {
                if (!SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString()))
                {
                    CurrentAutoStarts.Add(autoStart);
                }
            }
            HistoryAutoStarts.Clear();
            foreach (var autoStart in AllHistoryAutoStarts)
            {
                if (!SettingsService.DisabledConnectors.Contains(autoStart.Category.ToString()))
                {
                    HistoryAutoStarts.Add(autoStart);
                }
            }
        }

        public bool TryGetCurrentAutoStart(Guid Id, out AutoStartEntry value) {
            Logger.LogTrace("TryGetCurrentAutoStart called for {AutoStartId}", Id);
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
            Logger.LogTrace("TryGetHistoryAutoStart called for {AutoStartId}", Id);
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
            Logger.LogTrace("ConfirmAdd called for {AutoStartId}", Id);
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry addedAutoStart)) {
                addedAutoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                HistoryAutoStartChange?.Invoke(addedAutoStart);
            }
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry currentAutoStart)) {
                currentAutoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                Confirm?.Invoke(currentAutoStart);
                CurrentAutoStartChange?.Invoke(currentAutoStart);
                Logger.LogInformation("Confirmed add of {@addedAutoStart}", addedAutoStart);
            }
        }

        public void ConfirmRemove(Guid Id) {
            Logger.LogTrace("ConfirmRemove called for {AutoStartId}", Id);
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry autoStart)) {
                autoStart.ConfirmStatus = ConfirmStatus.Confirmed;
                HistoryAutoStartChange?.Invoke(autoStart);
                Logger.LogInformation("Confirmed remove of {@autoStart}", autoStart);
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
            Logger.LogTrace("RemoveAutoStart called for {AutoStartId}", Id);
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry autoStart)) {
                RemoveAutoStart(autoStart);
            }
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.LogTrace("RemoveAutoStart called for {AutoStartId}", autoStart.Id);
            if (ConnectorService.CanBeEnabled(autoStart)) {
                // remove disabled status to allow new entries for example at the same registry key in the future
                ConnectorService.EnableAutoStart(autoStart);
            }
            ConnectorService.RemoveAutoStart(autoStart);
            autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            Logger.LogInformation("Removed {@autoStart}", autoStart);
        }

        public void DisableAutoStart(Guid Id) {
            Logger.LogTrace("DisableAutoStart called for {AutoStartId}", Id);
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry autoStart)) {
                DisableAutoStart(autoStart);
            }
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            Logger.LogTrace("DisableAutoStart called for {AutoStartId}", autoStart.Id);
            ConnectorService.DisableAutoStart(autoStart);
            autoStart.ConfirmStatus = ConfirmStatus.Disabled;
            Logger.LogInformation("Disabled {@autoStart}", autoStart);
        }

        public void AddAutoStart(Guid Id) {
            Logger.LogTrace("AddAutoStart called for {AutoStartId}", Id);
            if (TryGetHistoryAutoStart(Id, out AutoStartEntry autoStart)) {
                AddAutoStart(autoStart);
            }
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.LogTrace("AddAutoStart called for {AutoStartId}", autoStart.Id);
            ConnectorService.AddAutoStart(autoStart);
            try {
                ConnectorService.EnableAutoStart(autoStart);
            } catch (AlreadySetException) {

            }
            autoStart.ConfirmStatus = ConfirmStatus.Reverted;
            Logger.LogInformation("Added {@autoStart}", autoStart);
        }

        public void EnableAutoStart(Guid Id) {
            Logger.LogTrace("EnableAutoStart called");
            if (TryGetCurrentAutoStart(Id, out AutoStartEntry autoStart)) {
                EnableAutoStart(autoStart);
            }
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            Logger.LogTrace("EnableAutoStart called for {AutoStartId}", autoStart.Id);
            ConnectorService.EnableAutoStart(autoStart);
            autoStart.ConfirmStatus = ConfirmStatus.Enabled;
            Logger.LogInformation("Enabled {@autoStart}", autoStart);
        }

        public bool CanAutoStartBeEnabled(AutoStartEntry autoStart) {
            return ConnectorService.CanBeEnabled(autoStart);
        }

        public bool CanAutoStartBeDisabled(AutoStartEntry autoStart) {
            return ConnectorService.CanBeDisabled(autoStart);
        }

        public bool CanAutoStartBeAdded(AutoStartEntry autoStart) {
            return ConnectorService.CanBeAdded(autoStart);
        }

        public bool CanAutoStartBeRemoved(AutoStartEntry autoStart) {
            return ConnectorService.CanBeRemoved(autoStart);
        }

        public async Task<bool> LoadCanBeAdded(AutoStartEntry autoStart) {
            autoStart.CanBeAddedLoader = Task<bool>.Run(() => {
                var oldValue = autoStart.CanBeAdded;
                var newValue = CanAutoStartBeAdded(autoStart);
                if (oldValue != newValue)
                {
                    autoStart.CanBeAdded = newValue;
                    CurrentAutoStartChange?.Invoke(autoStart);
                    HistoryAutoStartChange?.Invoke(autoStart);
                }
                return newValue;
            });
            return await autoStart.CanBeAddedLoader;
        }

        public async Task<bool> LoadCanBeRemoved(AutoStartEntry autoStart)
        {
            autoStart.CanBeRemovedLoader = Task<bool>.Run(() => {
                var oldValue = autoStart.CanBeRemoved;
                var newValue = CanAutoStartBeRemoved(autoStart);
                if (oldValue != newValue)
                {
                    autoStart.CanBeRemoved = newValue;
                    CurrentAutoStartChange?.Invoke(autoStart);
                    HistoryAutoStartChange?.Invoke(autoStart);
                }
                return newValue;
            });
            return await autoStart.CanBeRemovedLoader;
        }

        public async Task<bool> LoadCanBeEnabled(AutoStartEntry autoStart)
        {
            autoStart.CanBeEnabledLoader = Task<bool>.Run(() => {
                var oldValue = autoStart.CanBeEnabled;
                var newValue = CanAutoStartBeEnabled(autoStart);
                if (oldValue != newValue)
                {
                    autoStart.CanBeEnabled = newValue;
                    CurrentAutoStartChange?.Invoke(autoStart);
                    HistoryAutoStartChange?.Invoke(autoStart);
                }
                return newValue;
            });
            return await autoStart.CanBeEnabledLoader;
        }

        public async Task<bool> LoadCanBeDisabled(AutoStartEntry autoStart)
        {
            autoStart.CanBeDisabledLoader = Task<bool>.Run(() => {
                var oldValue = autoStart.CanBeDisabled;
                var newValue = CanAutoStartBeDisabled(autoStart);
                if (oldValue != newValue)
                {
                    autoStart.CanBeDisabled = newValue;
                    CurrentAutoStartChange?.Invoke(autoStart);
                    HistoryAutoStartChange?.Invoke(autoStart);
                }
                return newValue;
            });
            return await autoStart.CanBeDisabledLoader;
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
            autoStartValue.CanBeAddedLoader = null;
            autoStartValue.CanBeRemovedLoader = null;
            autoStartValue.CanBeEnabledLoader = null;
            autoStartValue.CanBeDisabledLoader = null;
        }

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            Logger.LogTrace("IsAdminRequiredForChanges called");
            return ConnectorService.IsAdminRequiredForChanges(autoStart);
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.LogTrace("GetCurrentAutoStarts called");
            return ConnectorService.GetCurrentAutoStarts();
        }

        public bool GetValidAutoStartFileExists() {
            Logger.LogTrace("GetAutoStartFileExists called");
            if (!File.Exists(PathToLastAutoStarts)) {
                return false;
            }
            try {
                GetSavedAutoStarts(PathToLastAutoStarts);
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public ObservableCollection<AutoStartEntry> GetSavedAutoStarts(string path) {
            try {
                Logger.LogTrace("Loading auto starts from file {path}", path);
                if (File.Exists($"{path}.xml"))
                {
                    var file = $"{path}.xml";
                    Logger.LogInformation($"Loading new xml serialized file \"{file}\"");
                    using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<AutoStartEntry>));
                        try
                        {
                            var ret = (ObservableCollection<AutoStartEntry>)serializer.Deserialize(stream);
                            Logger.LogTrace($"Loaded last saved auto starts from file \"{file}\"");
                            return ret;
                        }
                        catch (Exception ex)
                        {
                            var err = new Exception($"Failed to deserialize from file \"{file}\"", ex);
                            throw err;
                        }
                    }
                }
                else if (File.Exists($"{path}.bin"))
                {
                    var file = $"{path}.bin";
                    Logger.LogInformation($"Loading old binary serialized file \"{file}\"");
                    using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        try
                        {
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                            var ret = (ObservableCollection<AutoStartEntry>)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                            Logger.LogTrace($"Loaded last saved auto starts from file \"{file}\"");
                            return ret;
                        }
                        catch (Exception ex)
                        {
                            var err = new Exception($"Failed to deserialize from file \"{file}\"", ex);
                            throw err;
                        }
                    }
                }
                else
                {
                    return new ObservableCollection<AutoStartEntry>();
                }
            } catch (Exception ex) {
                var message = "Failed to load last auto starts";
                Logger.LogError(ex, message);
                throw new Exception(message, ex); ;
            }
        }

        public void SaveAutoStarts() {
            try {
                Logger.LogInformation("Saving current known auto starts");
                Logger.LogTrace("Saving current auto starts to file {path}", PathToLastAutoStarts);
                SaveAutoStarts(PathToLastAutoStarts, AllCurrentAutoStarts);
                Logger.LogTrace("Saving history auto starts to file {path}", PathToHistoryAutoStarts);
                SaveAutoStarts(PathToHistoryAutoStarts, AllHistoryAutoStarts);
                Logger.LogInformation("Saved all auto starts");
            } catch (Exception ex) {
                var message = "Failed to save current auto starts";
#pragma warning disable CA2254 // Template should be a static expression
                Logger.LogError(ex, message);
#pragma warning restore CA2254 // Template should be a static expression
                throw new Exception(message, ex); ;
            }
        }

        private void SaveAutoStarts(string path, ObservableCollection<AutoStartEntry> dictionary) {
            Logger.LogTrace("Saving auto starts to file {path}", path);
            try {
                try {
                    var folderPath = PathToLastAutoStarts.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(folderPath);
                } catch (Exception ex) {
                    var err = new Exception($"Failed to create folder for file \"{path}\"", ex);
                    throw err;
                }
                try {
                    using (Stream stream = new FileStream($"{path}.xml", FileMode.Create, FileAccess.Write, FileShare.None)) {
                        XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<AutoStartEntry>));
                        serializer.Serialize(stream, dictionary);
                    }
                } catch (Exception ex) {
                    var err = new Exception($"Failed to write file \"{path}\"", ex);
                    throw err;
                }
                Logger.LogTrace($"Saved auto starts to file \"{path}\"");
            } catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save auto starts to file {path}", path);
                throw new Exception($"Failed to save auto starts to file \"{path}\"", ex);
            }
        }

        /// <summary>
        /// Loads current autostarts, compares to last saved and fires add or remove events if necessary
        /// </summary>
        public void LoadCurrentAutoStarts() {
            try {
                Logger.LogInformation("Comparing current auto starts to last saved");

                // get last saved auto starts
                ObservableCollection<AutoStartEntry> lastSavedAutoStarts;
                try {
                    lastSavedAutoStarts = GetSavedAutoStarts(PathToLastAutoStarts);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Failed to load last saved auto starts");
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
                    allHistoryAutoStarts = GetSavedAutoStarts(PathToHistoryAutoStarts);
                } catch (Exception ex) {
                    Logger.LogError(ex, "Failed to load removed auto starts");
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
                Logger.LogTrace("LoadCurrentAutoStarts finished");
            } catch (Exception ex) {
                var message = "Failed to compare current auto starts to last saved";
#pragma warning disable CA2254 // Template should be a static expression
                Logger.LogError(ex, message);
#pragma warning restore CA2254 // Template should be a static expression
                throw new Exception(message, ex);
            }
        }

        public void StartWatcher() {
            Logger.LogInformation("Starting watchers");
            ConnectorService.StartWatcher();
            Logger.LogInformation("Started watchers");
        }

        public void StopWatcher() {
            Logger.LogTrace("Stopping watchers");
            ConnectorService.StopWatcher();
            Logger.LogTrace("Stopped watchers");
        }

        public bool IsOwnAutoStart(AutoStartEntry autoStart) {
            return autoStart.Category == Category.CurrentUserRun64 &&
            autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
            autoStart.Value == CurrentExePath;
        }


        public void ToggleOwnAutoStart() {
            try {
                Logger.LogInformation("ToggleOwnAutoStart called");
                var ownAutoStart = new RegistryAutoStartEntry() {
                    Category = Category.CurrentUserRun64,
                    Path = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm",
                    Value = CurrentExePath,
                    RegistryValueKind = Microsoft.Win32.RegistryValueKind.String,
                    ConfirmStatus = ConfirmStatus.New,
                };

                if (HasOwnAutoStart) {
                    Logger.LogInformation("Shall remove own auto start");
                    RemoveAutoStart(ownAutoStart);
                } else {
                    Logger.LogInformation("Shall add own auto start");
                    AddAutoStart(ownAutoStart);
                }
                ownAutoStart.ConfirmStatus = ConfirmStatus.New;
                Logger.LogTrace("Own auto start toggled");
            } catch (Exception e) {
                Logger.LogError(e, "Failed to toggle own auto start");
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
            DispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start added: {@value}", autostart);
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
                    Logger.LogTrace("AddHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Add handler failed");
                }
            });
        }

        private void EnableHandler(AutoStartEntry autostart)
        {
            DispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start enabled: {@value}", autostart);
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
                    Logger.LogTrace("EnableHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Enable handler failed");
                }
            });
        }

        private void DisableHandler(AutoStartEntry autostart)
        {
            DispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start disabled: {@value}", autostart);
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
                    Logger.LogTrace("DisableHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Disable handler failed");
                }
            });
        }

        private void RemoveHandler(AutoStartEntry autostart)
        {
            DispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    Logger.LogInformation("Auto start removed: {@value}", autostart);
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
                    Logger.LogTrace("RemoveHandler finished");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Remove handler failed");
                }
            });
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    ConnectorService.Add -= AddHandler;
                    ConnectorService.Remove -= RemoveHandler;
                    ConnectorService.Enable -= EnableHandler;
                    ConnectorService.Disable -= DisableHandler;
                    SettingsService.SettingsSaving -= SettingsSavingHandler;
                    SettingsService.SettingsLoaded -= SettingsLoadedHandler;
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
