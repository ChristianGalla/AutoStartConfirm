using AutoStartConfirm.AutoStarts;
using AutoStartConfirm.Exceptions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {
    class RegistryDisableService: IDisposable {

        public string DisableBasePath { get; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected static readonly byte[] enabledByteArray = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        protected static readonly byte[] disabledByteArray = { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        private bool disposedValue;

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

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RegistryDisableService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
