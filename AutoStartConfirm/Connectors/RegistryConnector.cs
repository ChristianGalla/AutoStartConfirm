using System;
using System.Collections;
using System.Collections.Generic;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.AutoStarts;
using Microsoft.Win32;
using System.Windows;

namespace AutoStartConfirm.Connectors {
    abstract class RegistryConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public abstract string BasePath { get; }

        public abstract string[] SubKeys { get; }

        public abstract bool MonitorSubkeys { get; }

        protected RegistryChangeMonitor monitor = null;

        protected IList<AutoStartEntry> lastAutostarts = null;

        private void ChangeHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Trace("ChangeHandler called for {BasePath}", BasePath);
            var newAutostarts = GetCurrentAutoStarts();
            var addedAutostarts = new List<AutoStartEntry>();
            var removedAutostarts = new List<AutoStartEntry>();
            foreach (var newAutostart in newAutostarts) {
                var found = false;
                foreach (var lastAutostart in lastAutostarts) {
                    if (newAutostart.Equals(lastAutostart)) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    addedAutostarts.Add(newAutostart);
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
            lastAutostarts = newAutostarts;
        }

        private void ErrorHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Trace("ErrorHandler called");
        }

        #region IAutoStartConnector implementation
        public abstract Category Category { get; }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called for {BasePath}", BasePath);
            try {
                var ret = new List<AutoStartEntry>();

                var currentKeys = new List<string>();

                if (MonitorSubkeys) {
                    RegistryKey registryKey;
                    if (BasePath.StartsWith("HKEY_LOCAL_MACHINE")) {
                        registryKey = Registry.LocalMachine;
                    } else {
                        throw new ArgumentOutOfRangeException($"Unknown registry base path for {BasePath}");
                    }
                    var paths = new List<string>();
                    if (SubKeys != null) {
                        foreach (var category in SubKeys) {
                            paths.Add($"{BasePath}\\{category}");
                        }
                    } else {
                        paths.Add(BasePath);
                    }

                    foreach (var path in paths) {
                        using (RegistryKey rootKey = registryKey.OpenSubKey(path)) {
                            if (rootKey != null) {
                                string[] valueNames = rootKey.GetValueNames();
                                foreach (string currSubKey in valueNames) {
                                    currentKeys.Add(currSubKey);
                                }
                                rootKey.Close();
                            }

                        }
                    }
                } else {
                    if (SubKeys != null) {
                        currentKeys.AddRange(SubKeys);
                    }
                }

                if (currentKeys != null) {
                    foreach (var currentKey in currentKeys) {
                        try {
                            object value = Registry.GetValue(BasePath, currentKey, null);
                            if (value == null) {
                                continue;
                            }
                            if (!(value is string) && value is IEnumerable) {
                                foreach (var subValue in value as IEnumerable) {
                                    var subValueAsString = subValue.ToString();
                                    if (subValueAsString.Length > 0) {
                                        ret.Add(new AutoStartEntry {
                                            Category = Category,
                                            Value = subValue.ToString(),
                                            Path = $"{BasePath}\\{currentKey}",
                                        });
                                    }
                                }
                            } else {
                                var valueAsString = value.ToString();
                                if (valueAsString.Length > 0) {
                                    ret.Add(new AutoStartEntry {
                                        Category = Category,
                                        Value = value.ToString(),
                                        Path = $"{BasePath}\\{currentKey}",
                                    });
                                }
                            }
                        } catch (Exception ex) {
                            var err = new Exception($"Failed to get category {currentKey}", ex);
                            throw err;
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
            lastAutostarts = GetCurrentAutoStarts();
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

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("AddAutoStart called for {Path}", autoStart.Path);
            var lastDelimiterPos = autoStart.Path.LastIndexOf('\\');
            var basePath = autoStart.Path.Substring(0, lastDelimiterPos);
            var category = autoStart.Path.Substring(lastDelimiterPos + 1);
            try {
                object value = Registry.GetValue(basePath, category, null);
                var newValues = new List<string> {
                    autoStart.Value
                };
                if (value == null) {
                    Registry.SetValue(basePath, category, newValues.ToArray(), RegistryValueKind.MultiString);
                } else if (value is IEnumerable<string>) {
                    bool exists = false;
                    foreach (var subValue in value as IEnumerable<string>) {
                        newValues.Add(subValue);
                        if (string.Equals(subValue, autoStart.Value, StringComparison.OrdinalIgnoreCase)) {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) {
                        Registry.SetValue(basePath, category, newValues.ToArray(), RegistryValueKind.MultiString);
                    }
                } else {
                    throw new Exception($"Don't know how to handle data type of key {autoStart.Path}");
                }
            } catch (Exception ex) {
                var err = new Exception($"Failed to add auto start {autoStart.Value} to {autoStart.Path}", ex);
                throw err;
            }
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("RemoveAutoStart called for {Path}", autoStart.Path);
            var lastDelimiterPos = autoStart.Path.LastIndexOf('\\');
            var basePath = autoStart.Path.Substring(0, lastDelimiterPos);
            var category = autoStart.Path.Substring(lastDelimiterPos + 1);
            try {
                object value = Registry.GetValue(basePath, category, null);
                var newValues = new List<string>();
                if (value == null) {
                    return;
                } else if (value is IEnumerable<string>) {
                    bool exists = false;
                    foreach (var subValue in value as IEnumerable<string>) {
                        if (string.Equals(subValue, autoStart.Value, StringComparison.OrdinalIgnoreCase)) {
                            exists = true;
                        } else {
                            newValues.Add(subValue);
                        }
                    }
                    if (exists) {
                        Registry.SetValue(basePath, category, newValues.ToArray(), RegistryValueKind.MultiString);
                    }
                } else {
                    throw new Exception($"Don't know how to handle data type of key {autoStart.Path}");
                }
            } catch (Exception ex) {
                var err = new Exception($"Failed to remove auto start {autoStart.Value} from {autoStart.Path}", ex);
                throw err;
            }
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
        #endregion
    }
}
