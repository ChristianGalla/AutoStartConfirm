using AutoStartConfirm.AutoStarts;
using AutoStartConfirm.Exceptions;
using AutoStartConfirm.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {
    #region Delegates
    public delegate void EnableChangeHandler(string name);
    #endregion

    class RegistryDisableService : IDisposable {

        public string DisableBasePath { get; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected static readonly byte[] enabledByteArray = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        protected static readonly byte[] disabledByteArray = { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        private Dictionary<string, bool> lastEnableStatus;
        protected RegistryChangeMonitor monitor;

        public RegistryDisableService(string DisableBasePath) {
            this.DisableBasePath = DisableBasePath;
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

        public void ToggleAutoStartEnable(AutoStartEntry autoStart, bool enable, bool dryRun) {
            Logger.Trace("ToggleAutoStartEnable called for {Value} in {Path} (enable: {Enable}, dryRun: {DryRun})", autoStart.Value, autoStart.Path, enable, dryRun);
            if (!(autoStart is FolderAutoStartEntry)) {
                throw new ArgumentException("Parameter must be of type FolderAutoStartEntry");
            }
            var firstDelimiterPos = DisableBasePath.IndexOf('\\');
            var subKeyPath = DisableBasePath.Substring(firstDelimiterPos + 1);
            var valueName = autoStart.Value;
            using (var registry = GetBaseRegistry(DisableBasePath))
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
                    Registry.SetValue(DisableBasePath, valueName, enabledByteArray, RegistryValueKind.Binary);
                } else {
                    Registry.SetValue(DisableBasePath, valueName, disabledByteArray, RegistryValueKind.Binary);
                }
            }
        }

        private void ChangeHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Trace("ChangeHandler called for {DisableBasePath}", DisableBasePath);
            var newEnableStatus = GetCurrentEnableStatus();
            foreach (var newStatus in newEnableStatus) {
                var name = newStatus.Key;
                var nowEnabled = newStatus.Value;
                var wasEnabled = true;
                if (lastEnableStatus.ContainsKey(name)) {
                    wasEnabled = lastEnableStatus[name];
                }
                if (wasEnabled != nowEnabled) {
                    if (nowEnabled) {
                        Enable?.Invoke(name);
                    } else {
                        Disable?.Invoke(name);
                    }
                }
            }
            foreach (var lastStatus in lastEnableStatus) {
                var name = lastStatus.Key;
                var wasEnabled = lastStatus.Value;
                if (newEnableStatus.ContainsKey(name)) {
                    continue;
                }
                if (!wasEnabled) {
                    Enable?.Invoke(name);
                }
            }
            lastEnableStatus = newEnableStatus;
        }

        public Dictionary<string, bool> GetCurrentEnableStatus() {
            Logger.Trace("GetCurrentEnableStatus called");
            var firstDelimiterPos = DisableBasePath.IndexOf('\\');
            var subKeyPath = DisableBasePath.Substring(firstDelimiterPos + 1);
            var ret = new Dictionary<string, bool>();
            using (var registry = GetBaseRegistry(DisableBasePath))
            using (var key = registry.OpenSubKey(subKeyPath, false)) {
                if (key == null) {
                    return ret;
                }
                var valueNames = key.GetValueNames();
                foreach (var valueName in valueNames) {
                    object currentValue = key.GetValue(valueName, null);
                    if (currentValue == null) {
                        continue;
                    }
                    var currentValueKind = key.GetValueKind(valueName);
                    if (currentValueKind != RegistryValueKind.Binary) {
                        continue;
                    }
                    var currentValueByteArray = (byte[])currentValue;
                    var isEnabled = currentValueByteArray[0] == enabledByteArray[0] && currentValueByteArray[11] == enabledByteArray[11];
                    if (isEnabled) {
                        ret.Add(valueName, true);
                    } else {
                        var isDisabled = currentValueByteArray[0] == disabledByteArray[0] && currentValueByteArray[11] == disabledByteArray[11];
                        if (isDisabled) {
                            ret.Add(valueName, false);
                        }
                    }
                }
            }
            return ret;
        }

        private void ErrorHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Error("Error on monitoring of {DisableBasePath}: {@Exception}", DisableBasePath, e);
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
            Logger.Trace("StartWatcher called for {DisableBasePath}", DisableBasePath);
            StopWatcher();
            lastEnableStatus = GetCurrentEnableStatus();
            monitor = new RegistryChangeMonitor(DisableBasePath);
            monitor.Changed += ChangeHandler;
            monitor.Error += ErrorHandler;
            monitor.Start();
            Logger.Trace("Watcher started");
        }

        public void StopWatcher() {
            Logger.Trace("StopWatcher called for {DisableBasePath}", DisableBasePath);
            if (monitor == null) {
                Logger.Trace("No watcher running");
                return;
            }
            Logger.Trace("Stopping watcher");
            monitor.Dispose();
            monitor = null;
        }

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
        public event EnableChangeHandler Enable;
        public event EnableChangeHandler Disable;
        #endregion
    }
}
