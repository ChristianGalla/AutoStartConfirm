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

    class RegistryDisableService : IDisposable, IRegistryDisableService {

        public string DisableBasePath { get; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected static readonly byte[] defaultEnabledByteArray = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        protected static readonly byte[] defaultDisabledByteArray = { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        private Dictionary<string, bool> lastEnableStatus;
        protected IRegistryChangeMonitor monitor;

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
            var firstDelimiterPos = DisableBasePath.IndexOf('\\');
            var subKeyPath = DisableBasePath.Substring(firstDelimiterPos + 1);
            string valueName;
            if (autoStart is FolderAutoStartEntry) {
                valueName = autoStart.Value;
            } else if (autoStart is RegistryAutoStartEntry) {
                valueName = autoStart.Path.Substring(autoStart.Path.LastIndexOf('\\') + 1);
            } else {
                throw new NotImplementedException();
            }
            using (var registry = GetBaseRegistry(DisableBasePath))
            using (var key = registry.OpenSubKey(subKeyPath, !dryRun)) {
                if (key == null && dryRun) {
                    return;
                }
                object currentValue = key.GetValue(valueName, null);
                byte[] currentValueByteArray = null;
                if (currentValue == null) {
                    if (enable) {
                        throw new AlreadySetException($"Auto start already enabled");
                    }
                    currentValueByteArray = defaultEnabledByteArray;
                } else if (currentValue != null) {
                    var currentValueKind = key.GetValueKind(valueName);
                    if (currentValueKind != RegistryValueKind.Binary) {
                        throw new ArgumentException($"Registry value has the wrong type \"{currentValueKind}\"");
                    }
                    currentValueByteArray = (byte[])currentValue;
                    var isEnabled = GetIsEnabled(currentValueByteArray);
                    if (enable && isEnabled) {
                        throw new AlreadySetException($"Auto start already enabled");
                    } else if (!enable && !isEnabled) {
                        throw new AlreadySetException($"Auto start already disabled");
                    }
                }
                if (dryRun) {
                    return;
                }
                if (enable) {
                    Registry.SetValue(DisableBasePath, valueName, GetEnabledValue(currentValueByteArray), RegistryValueKind.Binary);
                } else {
                    Registry.SetValue(DisableBasePath, valueName, GetDisabledValue(currentValueByteArray), RegistryValueKind.Binary);
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
                    var isEnabled = GetIsEnabled(currentValueByteArray);
                    if (isEnabled) {
                        ret.Add(valueName, true);
                    } else {
                        ret.Add(valueName, false);
                    }
                }
            }
            return ret;
        }

        private static bool GetIsEnabled(byte[] currentValueByteArray) {
            // enabled if most and least significant bytes are even
            return (currentValueByteArray[0] & 0b1) == 0 && (currentValueByteArray[11] & 0b1) == 0;
        }

        private static byte[] GetEnabledValue(byte[] currentValueByteArray) {
            // enabled if most and least significant bytes are not even
            // also all other bytes should be 0
            var firstByteAsInt = currentValueByteArray[0] & 0b_1111_1110;
            currentValueByteArray[0] = (byte)firstByteAsInt;
            for (int i = 1; i < currentValueByteArray.Length; i++) {
                currentValueByteArray[i] = 0b0;
            }
            return currentValueByteArray;
        }

        private static byte[] GetDisabledValue(byte[] currentValueByteArray) {
            // disabled if most and least significant bytes are not even
            // other bytes are maybe a timestamp when disabled via task manager, but this is not relevant to disable the auto start
            currentValueByteArray[0] |= 0b1;
            currentValueByteArray[11] |= 0b1;
            return currentValueByteArray;
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
