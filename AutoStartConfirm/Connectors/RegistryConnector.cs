using System;
using System.Collections;
using System.Collections.Generic;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.AutoStarts;
using Microsoft.Win32;
using System.Windows;

namespace AutoStartConfirm.Connectors {
    class RegistryConnector : IAutoStartConnector, IDisposable {
        public Category Category;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string basePath;

        public string[] categories;

        protected RegistryChangeMonitor monitor = null;

        protected IList<AutoStartEntry> lastAutostarts = null;

        private void ChangeHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Trace("ChangeHandler called for {basePath}", basePath);
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
        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called for {basePath}", basePath);
            try {
                var ret = new List<AutoStartEntry>();

                var currentCategories = new List<string>();
                if (categories != null) {
                    currentCategories.AddRange(categories);
                }

                if (currentCategories.Count == 0) {
                    RegistryKey registryKey;
                    if (basePath.StartsWith("HKEY_LOCAL_MACHINE")) {
                        registryKey = Registry.LocalMachine;
                    } else {
                        throw new ArgumentOutOfRangeException($"Unknown registry base path for {basePath}");
                    }
                    using (RegistryKey rootKey = registryKey.OpenSubKey(basePath)) {
                        if (rootKey != null) {
                            string[] valueNames = rootKey.GetValueNames();
                            foreach (string currSubKey in valueNames) {
                                currentCategories.Add(currSubKey);
                            }
                            rootKey.Close();
                        }

                    }
                }

                if (currentCategories != null) {
                    foreach (var category in currentCategories) {
                        try {
                            object value = Registry.GetValue(basePath, category, null);
                            if (value == null) {
                                continue;
                            }
                            if (value is IEnumerable) {
                                foreach (var subValue in value as IEnumerable) {
                                    ret.Add(new AutoStartEntry {
                                        Category = Category,
                                        Value = subValue.ToString(),
                                        Path = $"{basePath}\\{category}",
                                    });
                                }
                            } else {
                                ret.Add(new AutoStartEntry {
                                    Category = Category,
                                    Value = value.ToString(),
                                    Path = $"{basePath}\\{category}",
                                });
                            }
                        } catch (Exception ex) {
                            var err = new Exception($"Failed to get category {category}", ex);
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

        public void StartWatcher() {
            Logger.Trace("StartWatcher called for {basePath}",  basePath);
            StopWatcher();
            lastAutostarts = GetCurrentAutoStarts();
            monitor = new RegistryChangeMonitor(basePath);
            monitor.Changed += ChangeHandler;
            monitor.Error += ErrorHandler;
            monitor.Start();
            Logger.Trace("Watcher started");
        }

        public void StopWatcher() {
            Logger.Trace("StopWatcher called for {basePath}", basePath);
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
