using System;
using System.Collections;
using System.Collections.Generic;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.AutoStarts;
using Microsoft.Win32;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AutoStartConfirm.Exceptions;

namespace AutoStartConfirm.Connectors {
    abstract class RegistryConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public abstract string BasePath { get; }

        public abstract string[] SubKeyNames { get; }

        public abstract string[] ValueNames { get; }

        public abstract bool MonitorSubkeys { get; }

        public bool IsAdminRequiredForChanges {
            get {
                return BasePath.StartsWith("HKEY_LOCAL_MACHINE");
            }
        }

        protected virtual bool GetIsAutoStartEntry(RegistryKey currentKey, string valueName, int level) {
            return ValueNames == null || ValueNames.Contains(valueName);
        }

        protected RegistryChangeMonitor monitor = null;

        protected List<RegistryAutoStartEntry> lastAutostarts = null;

        // todo: Add Change handler for enable / disable keys
        private void ChangeHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Trace("ChangeHandler called for {BasePath}", BasePath);
            var newAutostarts = GetCurrentAutoStarts();
            var addedAutostarts = new List<RegistryAutoStartEntry>();
            var removedAutostarts = new List<RegistryAutoStartEntry>();
            foreach (var newAutostart in newAutostarts) {
                var found = false;
                foreach (var lastAutostart in lastAutostarts) {
                    if (newAutostart.Equals(lastAutostart)) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    addedAutostarts.Add((RegistryAutoStartEntry)newAutostart);
                }
            }
            foreach (var lastAutostart in lastAutostarts) {
                var found = false;
                foreach (var newAutostart in newAutostarts) {
                    if (newAutostart.Equals(lastAutostart)) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    removedAutostarts.Add(lastAutostart);
                }
            }
            foreach (var addedAutostart in addedAutostarts) {
                Application.Current.Dispatcher.Invoke(delegate {
                    Add?.Invoke(addedAutostart);
                });
            }
            foreach (var removedAutostart in removedAutostarts) {
                Application.Current.Dispatcher.Invoke(delegate {
                    Remove?.Invoke(removedAutostart);
                });
            }
            lastAutostarts.Clear();
            foreach (var newAutoStartEntry in newAutostarts) {
                lastAutostarts.Add((RegistryAutoStartEntry)newAutoStartEntry);
            }
        }

        private void ErrorHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Error("Error on monitoring of {BasePath}: {@Exception}", BasePath, e);
        }

        #region IAutoStartConnector implementation
        public abstract Category Category { get; }

        /// <summary>
        /// Gets all values inside the key as RegistryAutoStartEntry
        /// </summary>
        /// <param name="currentKey"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public IList<RegistryAutoStartEntry> GetCurrentAutoStarts(RegistryKey currentKey, bool recursive = false, int level = 0) {
            var ret = new List<RegistryAutoStartEntry>();
            var valueNames = currentKey.GetValueNames();
            foreach (var valueName in valueNames) {
                try {
                    var valueKind = currentKey.GetValueKind(valueName);
                    switch (valueKind) {
                        case RegistryValueKind.String:
                        case RegistryValueKind.ExpandString: {
                                string value = (string)currentKey.GetValue(valueName, null);
                                if (value == null) {
                                    continue;
                                }
                                if (value.Length > 0 && GetIsAutoStartEntry(currentKey, valueName, level)) {
                                    var newAutoStart = new RegistryAutoStartEntry {
                                        Category = Category,
                                        Value = value.ToString(),
                                        Path = $"{currentKey}\\{valueName}",
                                        RegistryValueKind = valueKind,
                                    };
                                    ret.Add(newAutoStart);
                                }
                                break;
                            }
                        case RegistryValueKind.MultiString: {
                                IEnumerable<string> value = (IEnumerable<string>)currentKey.GetValue(valueName, null);
                                if (value == null) {
                                    continue;
                                }
                                foreach (var subValue in value) {
                                    if (subValue.Length > 0 && GetIsAutoStartEntry(currentKey, valueName, level)) {
                                        var newAutoStart = new RegistryAutoStartEntry {
                                            Category = Category,
                                            Value = subValue.ToString(),
                                            Path = $"{currentKey}\\{valueName}",
                                            RegistryValueKind = valueKind,
                                        };
                                        ret.Add(newAutoStart);
                                    }
                                }
                                break;
                            }
                        case RegistryValueKind.Binary:
                        case RegistryValueKind.DWord:
                        case RegistryValueKind.QWord:
                        case RegistryValueKind.Unknown:
                        case RegistryValueKind.None:
                        default:
                            Logger.Trace("Skipping {valueName} from {currentKey} because of not implemented type {type}", valueName, currentKey, valueKind);
                            break;
                    }
                } catch (Exception ex) {
                    var err = new Exception($"Failed to get \"{valueName}\" from \"{currentKey}\"", ex);
                    throw err;
                }
            }
            if (recursive) {
                var subKeyNames = currentKey.GetSubKeyNames();
                foreach (var subKeyName in subKeyNames) {
                    using (var subKey = currentKey.OpenSubKey(subKeyName)) {
                        if (subKey != null) {
                            var subAutoStartEntries = GetCurrentAutoStarts(subKey, recursive, level + 1);
                            ret.AddRange(subAutoStartEntries);
                        }
                    }
                }
            }
            return ret;
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called for {BasePath}", BasePath);
            try {
                var ret = new List<AutoStartEntry>();
                using (RegistryKey baseRegistryKey = GetBaseRegistry()) {
                    var subKeyPath = BasePath.Substring(BasePath.IndexOf('\\') + 1);
                    using (RegistryKey rootKey = baseRegistryKey.OpenSubKey(subKeyPath)) {
                        if (rootKey != null) {
                            if (SubKeyNames != null) {
                                foreach (var subKeyName in SubKeyNames) {
                                    using (var subKey = rootKey.OpenSubKey(subKeyName)) {
                                        if (subKey == null) {
                                            continue;
                                        }
                                        var currentAutoStarts = GetCurrentAutoStarts(subKey, MonitorSubkeys);
                                        ret.AddRange(currentAutoStarts);
                                    }
                                }
                            } else {
                                var currentAutoStarts = GetCurrentAutoStarts(rootKey, MonitorSubkeys);
                                ret.AddRange(currentAutoStarts);
                            }
                        }
                    }
                }
                Logger.Trace("Got current auto starts");
                return ret;
            } catch (Exception ex) {
                var err = new Exception("Failed to get current auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        private RegistryKey GetBaseRegistry(string basePath) {
            RegistryKey registryKey;
            if (basePath.StartsWith("HKEY_LOCAL_MACHINE")) {
                registryKey = Registry.LocalMachine;
            } else if (basePath.StartsWith("HKEY_CURRENT_USER")) {
                registryKey = Registry.CurrentUser;
            } else {
                throw new ArgumentOutOfRangeException($"Unknown registry base path for \"{basePath}\"");
            }
            return registryKey;
        }

        private RegistryKey GetBaseRegistry() {
            return GetBaseRegistry(BasePath);
        }

        /// <summary>
        /// Watches the assigned registry keys
        /// </summary>
        /// <remarks>
        /// Because of API limitations no all changes are monitored.
        /// See https://docs.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regnotifychangekeyvalue
        /// Not monitored are changes via RegRestoreKey https://docs.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regrestorekeya
        /// </remarks>
        public void StartWatcher() {
            Logger.Trace("StartWatcher called for {BasePath}", BasePath);
            StopWatcher();
            var currentAutoStarts = (List<AutoStartEntry>)GetCurrentAutoStarts();
            lastAutostarts = new List<RegistryAutoStartEntry>();
            foreach (var currentAutoStart in currentAutoStarts) {
                lastAutostarts.Add((RegistryAutoStartEntry)currentAutoStart);
            }
            monitor = new RegistryChangeMonitor(BasePath);
            monitor.Changed += ChangeHandler;
            monitor.Error += ErrorHandler;
            monitor.Start();
            Logger.Trace("Watcher started");
        }

        public void StopWatcher() {
            Logger.Trace("StopWatcher called for {BasePath}", BasePath);
            if (monitor == null) {
                Logger.Trace("No watcher running");
                return;
            }
            Logger.Trace("Stopping watcher");
            monitor.Dispose();
            monitor = null;
        }

        public bool CanBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("CanBeAdded called for {Value} in {Path}", autoStart.Value, autoStart.Path);
            try {
                AddAutoStart(autoStart, true);
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("CanBeRemoved called for {Value} in {Path}", autoStart.Value, autoStart.Path);
            try {
                RemoveAutoStart(autoStart, true);
            } catch (Exception) {
                return false;
            }
            return true;
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            AddAutoStart(autoStart, false);
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            RemoveAutoStart(autoStart, false);
        }

        protected void AddAutoStart(AutoStartEntry autoStart, bool dryRun) {
            Logger.Trace("AddAutoStart called for {Value} in {Path} (dryRun: {DryRun})", autoStart.Value, autoStart.Path, dryRun);
            if (!(autoStart is RegistryAutoStartEntry)) {
                throw new ArgumentException("Parameter must be of type RegistryAutoStartEntry");
            }
            RegistryAutoStartEntry regAutoStart = (RegistryAutoStartEntry)autoStart;
            var firstDelimiterPos = regAutoStart.Path.IndexOf('\\');
            var lastDelimiterPos = regAutoStart.Path.LastIndexOf('\\');
            var keyPath = regAutoStart.Path.Substring(0, lastDelimiterPos);
            var subKeyPath = keyPath.Substring(firstDelimiterPos + 1);
            var valueName = regAutoStart.Path.Substring(lastDelimiterPos + 1);
            using (var registry = GetBaseRegistry())
            using (var key = registry.OpenSubKey(subKeyPath, !dryRun)) {
                if (key == null && dryRun) {
                    return;
                }
                object value = key.GetValue(valueName, null);
                if (value != null) {
                    var currentValueKind = key.GetValueKind(valueName);
                    if (currentValueKind != regAutoStart.RegistryValueKind) {
                        throw new ArgumentException($"Value \"{valueName}\" of key \"{keyPath}\" already exists with different type \"{currentValueKind}\"");
                    }
                }
                switch (regAutoStart.RegistryValueKind) {
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString: {
                            if (value != null) {
                                if (!string.Equals((string)value, regAutoStart.Value, StringComparison.OrdinalIgnoreCase)) {
                                    throw new AlreadySetByOtherException($"Value \"{valueName}\" of key \"{keyPath}\" already set to value \"{(string)value}\"");
                                } else {
                                    throw new AlreadySetException($"Value \"{valueName}\" of key \"{keyPath}\" already set");
                                }
                            }
                            if (dryRun) {
                                return;
                            }
                            Registry.SetValue(keyPath, valueName, regAutoStart.Value, regAutoStart.RegistryValueKind);
                            Logger.Info("Added {Value} to {Path}", regAutoStart.Value, regAutoStart.Path);
                            break;
                        }
                    case RegistryValueKind.MultiString: {
                            var newValues = new List<string> {
                                regAutoStart.Value
                            };
                            bool exists = false;
                            if (value != null) {
                                foreach (var subValue in value as IEnumerable<string>) {
                                    newValues.Add(subValue);
                                    if (string.Equals(subValue, regAutoStart.Value, StringComparison.OrdinalIgnoreCase)) {
                                        exists = true;
                                        break;
                                    }
                                }
                            }
                            if (!exists) {
                                if (dryRun) {
                                    return;
                                }
                                Registry.SetValue(keyPath, valueName, newValues.ToArray(), regAutoStart.RegistryValueKind);
                                Logger.Info("Added {Value} to {Path}", regAutoStart.Value, regAutoStart.Path);
                            } else {
                                throw new AlreadySetException($"\"{regAutoStart.Value}\" already exists at \"{regAutoStart.Path}\"");
                            }
                            break;
                        }
                    case RegistryValueKind.Binary:
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                    case RegistryValueKind.Unknown:
                    case RegistryValueKind.None:
                    default:
                        throw new ArgumentException($"Don't know how to handle data type \"{regAutoStart.RegistryValueKind}\" of key \"{regAutoStart.Path}\"");
                }
            }
        }

        protected void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun) {
            Logger.Trace("RemoveAutoStart called for {Value} in {Path} (dryRun: {DryRun})", autoStartEntry.Value, autoStartEntry.Path, dryRun);
            if (!(autoStartEntry is RegistryAutoStartEntry)) {
                throw new ArgumentException("Parameter must be of type RegistryAutoStartEntry");
            }
            var registryAutoStartEntry = (RegistryAutoStartEntry)autoStartEntry;
            var firstDelimiterPos = registryAutoStartEntry.Path.IndexOf('\\');
            var lastDelimiterPos = registryAutoStartEntry.Path.LastIndexOf('\\');
            var keyPath = registryAutoStartEntry.Path.Substring(0, lastDelimiterPos);
            var subKeyPath = keyPath.Substring(firstDelimiterPos + 1);
            var valueName = registryAutoStartEntry.Path.Substring(lastDelimiterPos + 1);
            using (var registry = GetBaseRegistry())
            using (var key = registry.OpenSubKey(subKeyPath, !dryRun)) {
                if (key == null) {
                    throw new ArgumentException("Path not found");
                }
                switch (key.GetValueKind(valueName)) {
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString: {
                            string value = (string)key.GetValue(valueName);
                            if (value == registryAutoStartEntry.Value) {
                                if (dryRun) {
                                    return;
                                }
                                key.DeleteValue(valueName);
                                Logger.Info("Removed {Value} from {Path}", registryAutoStartEntry.Value, registryAutoStartEntry.Path);
                            } else {
                                throw new ArgumentException("Value not found");
                            }
                            break;
                        }
                    case RegistryValueKind.MultiString: {
                            string[] value = (string[])key.GetValue(valueName);
                            bool exists = false;
                            var newValues = new Collection<string>();
                            foreach (var subValue in value as IEnumerable<string>) {
                                if (string.Equals(subValue, registryAutoStartEntry.Value, StringComparison.OrdinalIgnoreCase)) {
                                    exists = true;
                                } else {
                                    newValues.Add(subValue);
                                }
                            }
                            if (exists) {
                                if (dryRun) {
                                    return;
                                }
                                if (newValues.Count == 0) {
                                    key.DeleteValue(valueName);
                                } else {
                                    key.SetValue(valueName, newValues.ToArray(), RegistryValueKind.MultiString);
                                }
                                Logger.Info("Removed {Value} from {Path}", registryAutoStartEntry.Value, registryAutoStartEntry.Path);
                            } else {
                                throw new ArgumentException("Value not found");
                            }
                            break;
                        }
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                    case RegistryValueKind.Binary:
                    case RegistryValueKind.Unknown:
                    default:
                        throw new InvalidOperationException($"Don't know how to handle data type of key \"{registryAutoStartEntry.Path}\"");
                }
            }
        }

        // todo
        public bool CanBeEnabled(AutoStartEntry autoStart) {
            return false;
        }

        // todo
        public bool CanBeDisabled(AutoStartEntry autoStart) {
            return false;
        }

        // todo
        public void EnableAutoStart(AutoStartEntry autoStart) {
            throw new NotImplementedException();
        }

        // todo
        public void DisableAutoStart(AutoStartEntry autoStart) {
            throw new NotImplementedException();
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    StopWatcher();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        #endregion

        #region Events
        public event AutoStartChangeHandler Add;
        public event AutoStartChangeHandler Remove;
        public event AutoStartChangeHandler Enable;
        public event AutoStartChangeHandler Disable;
        #endregion
    }
}
