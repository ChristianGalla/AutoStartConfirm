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
using System.IO;
using AutoStartConfirm.Exceptions;

namespace AutoStartConfirm.Connectors {
    abstract class FolderConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public abstract string BasePath { get; }

        public abstract string EnableBasePath { get; }

        public abstract bool IsAdminRequiredForChanges {
            get;
        }

        protected FolderChangeMonitor monitor = null;

        // todo: read target of links?
        // read sub directories?
        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            string[] filePaths = Directory.GetFiles(BasePath);
            foreach (var filePath in filePaths) {
                var fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                if (fileName.ToLower() == "desktop.ini") {
                    continue;
                }
                var entry = new FolderAutoStartEntry() {
                    Category = Category,
                    Path = BasePath,
                    Value = fileName,
                };
                ret.Add(entry);
            }
            return ret;
        }


        #region IAutoStartConnector implementation
        public abstract Category Category { get; }

        public void StartWatcher() {
            Logger.Trace("StartWatcher called for {BasePath}", BasePath);
            StopWatcher();
            var currentAutoStarts = (List<AutoStartEntry>)GetCurrentAutoStarts();
            monitor = new FolderChangeMonitor() {
                BasePath = BasePath,
                Category = Category,
            };
            monitor.Add += AddHandler;
            monitor.Remove += RemoveHandler;
            monitor.Start();
            Logger.Trace("Watcher started");
        }

        private void RemoveHandler(AutoStartEntry e) {
            Logger.Trace("RemoveHandler called");
            Remove?.Invoke(e);
        }

        private void AddHandler(AutoStartEntry e) {
            Logger.Trace("AddHandler called");
            Add?.Invoke(e);
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
            Logger.Trace("CanBeAdded called for {autoStart}", autoStart);
            return false;
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("CanBeRemoved called for {autoStart}", autoStart);
            try {
                RemoveAutoStart(autoStart, true);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("AddAutoStart called for {autoStart}", autoStart);
            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry) {
            RemoveAutoStart(autoStartEntry, false);
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false) {
            Logger.Trace("RemoveAutoStart called for {autoStartEntry} (dryRun: {DryRun})", autoStartEntry, dryRun);
            if (autoStartEntry == null) {
                throw new ArgumentNullException("autoStartEntry is required");
            }
            if (autoStartEntry is FolderAutoStartEntry folderAutoStartEntry) {
                string fullPath = $"{folderAutoStartEntry.Path}{Path.DirectorySeparatorChar}{folderAutoStartEntry.Value}";
                if (File.Exists(fullPath)) {
                    if (dryRun) {
                        return;
                    }
                    File.Delete(fullPath);
                    Logger.Info("Removed {Value} from {Path}", folderAutoStartEntry.Value, folderAutoStartEntry.Path);
                } else {
                    throw new FileNotFoundException($"File \"{fullPath}\" not found");
                }
            } else {
                throw new ArgumentException("autoStartEntry is not of type folderAutoStartEntry");
            }
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            try {
                EnableAutoStart(autoStart, true);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            try {
                DisableAutoStart(autoStart, true);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            EnableAutoStart(autoStart, false);
        }

        public void EnableAutoStart(AutoStartEntry autoStart, bool dryRun) {
            ToggleAutoStartEnable(autoStart, true, dryRun);
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            DisableAutoStart(autoStart, false);
        }

        public void DisableAutoStart(AutoStartEntry autoStart, bool dryRun) {
            ToggleAutoStartEnable(autoStart, false, dryRun);
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

        protected static readonly byte[] enabledByteArray = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        protected static readonly byte[] disabledByteArray = { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        public void ToggleAutoStartEnable(AutoStartEntry autoStart, bool enable, bool dryRun) {
            Logger.Trace("ToggleAutoStartEnable called for {Value} in {Path} (enable: {Enable}, dryRun: {DryRun})", autoStart.Value, autoStart.Path, enable, dryRun);
            if (!(autoStart is FolderAutoStartEntry)) {
                throw new ArgumentException("Parameter must be of type FolderAutoStartEntry");
            }
            var firstDelimiterPos = EnableBasePath.IndexOf('\\');
            var subKeyPath = EnableBasePath.Substring(firstDelimiterPos + 1);
            var valueName = autoStart.Value;
            using (var registry = GetBaseRegistry(EnableBasePath))
            using (var key = registry.OpenSubKey(subKeyPath, !dryRun)) {
                if (key == null && dryRun) {
                    return;
                }
                object currentValue = key.GetValue(valueName, null);
                if (currentValue != null) {
                    var currentValueKind = key.GetValueKind(valueName);
                    if (currentValueKind != RegistryValueKind.Binary) {
                        throw new ArgumentException($"Registry value has the wrong type \"{currentValueKind}\"");
                    }
                    var currentValueByteArray = (byte[])currentValue;
                    if (enable) {
                        var isEnabled = currentValueByteArray[0] == enabledByteArray[0] && currentValueByteArray[11] == enabledByteArray[11];
                        if (isEnabled) {
                            throw new AlreadySetException($"Auto start already enabled");
                        }
                    } else {
                        var isDisabled = currentValueByteArray[0] == disabledByteArray[0] && currentValueByteArray[11] == disabledByteArray[11];
                        if (isDisabled) {
                            throw new AlreadySetException($"Auto start already disabled");
                        }
                    }
                } else if (enable) {
                    throw new AlreadySetException($"Auto start already enabled");
                }
                if (dryRun) {
                    return;
                }
                if (enable) {
                    Registry.SetValue(EnableBasePath, valueName, enabledByteArray, RegistryValueKind.Binary);
                } else {
                    Registry.SetValue(EnableBasePath, valueName, disabledByteArray, RegistryValueKind.Binary);
                }
            }
        }

        // todo
        public bool IsEnabled(AutoStartEntry autoStart) {
            return true;
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
