using AutoStartConfirm.Exceptions;
using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Management;

namespace AutoStartConfirm.Connectors
{
    #region Delegates
    public delegate void EnableChangeHandler(string name);
    #endregion

    public class RegistryDisableService : IDisposable, IRegistryDisableService
    {

        private string? disableBasePath;

        public string? DisableBasePath {
            get => disableBasePath;
            set {
                disableBasePath = value;
                RegistryChangeMonitor.RegistryPath = disableBasePath;
            }
        }

        private readonly ILogger<RegistryDisableService> Logger;

        protected static readonly byte[] defaultEnabledByteArray = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        protected static readonly byte[] defaultDisabledByteArray = { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        private Dictionary<string, bool>? lastEnableStatus;
        protected IRegistryChangeMonitor RegistryChangeMonitor;

        public RegistryDisableService(ILogger<RegistryDisableService> logger, IRegistryChangeMonitor registryChangeMonitor)
        {
            Logger = logger;
            RegistryChangeMonitor = registryChangeMonitor;
            RegistryChangeMonitor.Changed += ChangeHandler;
        }

        private static RegistryKey GetBaseRegistry(string basePath)
        {
            RegistryKey registryKey;
            if (basePath.StartsWith("HKEY_LOCAL_MACHINE"))
            {
                registryKey = Microsoft.Win32.Registry.LocalMachine;
            }
            else if (basePath.StartsWith("HKEY_CURRENT_USER"))
            {
                registryKey = Microsoft.Win32.Registry.CurrentUser;
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Unknown registry base path for \"{basePath}\"");
            }
            return registryKey;
        }

        public bool CanBeEnabled(AutoStartEntry autoStart)
        {
            try
            {
                EnableAutoStart(autoStart, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CanBeDisabled(AutoStartEntry autoStart)
        {
            try
            {
                DisableAutoStart(autoStart, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void EnableAutoStart(AutoStartEntry autoStart)
        {
            EnableAutoStart(autoStart, false);
        }

        public void EnableAutoStart(AutoStartEntry autoStart, bool dryRun)
        {
            ToggleAutoStartEnable(autoStart, true, dryRun);
        }

        public void DisableAutoStart(AutoStartEntry autoStart)
        {
            DisableAutoStart(autoStart, false);
        }

        public void DisableAutoStart(AutoStartEntry autoStart, bool dryRun)
        {
            ToggleAutoStartEnable(autoStart, false, dryRun);
        }

        public void ToggleAutoStartEnable(AutoStartEntry autoStart, bool enable, bool dryRun)
        {
            Logger.LogTrace("ToggleAutoStartEnable called for {Value} in {Path} (enable: {Enable}, dryRun: {DryRun})", autoStart.Value, autoStart.Path, enable, dryRun);
            if (DisableBasePath == null)
            {
                throw new InvalidOperationException("DisableBasePath not set");
            }
            var firstDelimiterPos = DisableBasePath.IndexOf('\\');
            var subKeyPath = DisableBasePath[(firstDelimiterPos + 1)..];
            string valueName;
            if (autoStart is FolderAutoStartEntry)
            {
                valueName = autoStart.Value;
            }
            else if (autoStart is RegistryAutoStartEntry)
            {
                valueName = autoStart.Path[(autoStart.Path.LastIndexOf('\\') + 1)..];
            }
            else
            {
                throw new NotImplementedException();
            }
            using var registry = GetBaseRegistry(DisableBasePath);
            using var key = registry.OpenSubKey(subKeyPath, !dryRun) ?? throw new ArgumentException($"Failed to get key \"{subKeyPath}\"");
            object? currentValue = key.GetValue(valueName, null);
            byte[]? currentValueByteArray = null;
            if (currentValue == null)
            {
                if (enable)
                {
                    throw new AlreadySetException($"Auto start already enabled");
                }
                currentValueByteArray = defaultEnabledByteArray;
            }
            else
            {
                var currentValueKind = key.GetValueKind(valueName);
                if (currentValueKind != RegistryValueKind.Binary)
                {
                    throw new ArgumentException($"Registry value has the wrong type \"{currentValueKind}\"");
                }
                currentValueByteArray = (byte[])currentValue;
                var isEnabled = GetIsEnabled(currentValueByteArray);
                if (enable && isEnabled)
                {
                    throw new AlreadySetException($"Auto start already enabled");
                }
                else if (!enable && !isEnabled)
                {
                    throw new AlreadySetException($"Auto start already disabled");
                }
            }
            if (dryRun)
            {
                return;
            }
            if (enable)
            {
                Microsoft.Win32.Registry.SetValue(DisableBasePath, valueName, GetEnabledValue(currentValueByteArray), RegistryValueKind.Binary);
            }
            else
            {
                Microsoft.Win32.Registry.SetValue(DisableBasePath, valueName, GetDisabledValue(currentValueByteArray), RegistryValueKind.Binary);
            }
        }

        private void ChangeHandler(object sender, EventArrivedEventArgs e)
        {
            Logger.LogTrace("ChangeHandler called for {DisableBasePath}", DisableBasePath);
            var newEnableStatus = GetCurrentEnableStatus();
            foreach (var newStatus in newEnableStatus)
            {
                var name = newStatus.Key;
                var nowEnabled = newStatus.Value;
                var wasEnabled = true;
                if (lastEnableStatus!.ContainsKey(name))
                {
                    wasEnabled = lastEnableStatus[name];
                }
                if (wasEnabled != nowEnabled)
                {
                    if (nowEnabled)
                    {
                        Enable?.Invoke(name);
                    }
                    else
                    {
                        Disable?.Invoke(name);
                    }
                }
            }
            foreach (var lastStatus in lastEnableStatus!)
            {
                var name = lastStatus.Key;
                var wasEnabled = lastStatus.Value;
                if (newEnableStatus.ContainsKey(name))
                {
                    continue;
                }
                if (!wasEnabled)
                {
                    Enable?.Invoke(name);
                }
            }
            lastEnableStatus = newEnableStatus;
        }

        public Dictionary<string, bool> GetCurrentEnableStatus()
        {
            Logger.LogTrace("GetCurrentEnableStatus called");
            if (DisableBasePath == null)
            {
                throw new InvalidOperationException("DisableBasePath not set");
            }
            var firstDelimiterPos = DisableBasePath.IndexOf('\\');
            var subKeyPath = DisableBasePath[(firstDelimiterPos + 1)..];
            var ret = new Dictionary<string, bool>();
            using (var registry = GetBaseRegistry(DisableBasePath))
            using (var key = registry.OpenSubKey(subKeyPath, false))
            {
                if (key == null)
                {
                    return ret;
                }
                var valueNames = key.GetValueNames();
                foreach (var valueName in valueNames)
                {
                    object? currentValue = key.GetValue(valueName, null);
                    if (currentValue == null)
                    {
                        continue;
                    }
                    var currentValueKind = key.GetValueKind(valueName);
                    if (currentValueKind != RegistryValueKind.Binary)
                    {
                        continue;
                    }
                    var currentValueByteArray = (byte[])currentValue;
                    var isEnabled = GetIsEnabled(currentValueByteArray);
                    if (isEnabled)
                    {
                        ret.Add(valueName, true);
                    }
                    else
                    {
                        ret.Add(valueName, false);
                    }
                }
            }
            return ret;
        }

        private static bool GetIsEnabled(byte[] currentValueByteArray)
        {
            // enabled if most and least significant bits are even
            return (currentValueByteArray[0] & 0b1) == 0 && (currentValueByteArray[11] & 0b1) == 0;
        }

        private static byte[] GetEnabledValue(byte[] currentValueByteArray)
        {
            // enabled if most and least significant bytes are not even
            // also all other bytes should be 0
            var firstByteAsInt = currentValueByteArray[0] & 0b_1111_1110;
            currentValueByteArray[0] = (byte)firstByteAsInt;
            for (int i = 1; i < currentValueByteArray.Length; i++)
            {
                currentValueByteArray[i] = 0b0;
            }
            return currentValueByteArray;
        }

        private static byte[] GetDisabledValue(byte[] currentValueByteArray)
        {
            // disabled if most and least significant bytes are not even
            // other bytes are maybe a timestamp when disabled via task manager, but this is not relevant to disable the auto start
            currentValueByteArray[0] |= 0b1;
            currentValueByteArray[11] |= 0b1;
            return currentValueByteArray;
        }

        /// <summary>
        /// Watches the assigned registry keys
        /// </summary>
        /// <remarks>
        /// Because of API limitations not all changes are monitored.
        /// See https://docs.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regnotifychangekeyvalue
        /// Not monitored are changes via RegRestoreKey https://docs.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regrestorekeya
        /// </remarks>
        public void StartWatcher()
        {
            Logger.LogTrace("StartWatcher called for {DisableBasePath}", DisableBasePath);
            if (DisableBasePath == null)
            {
                throw new InvalidOperationException("DisableBasePath not set");
            }
            lastEnableStatus = GetCurrentEnableStatus();
            RegistryChangeMonitor.Start();
            Logger.LogTrace("Watcher started");
        }

        public void StopWatcher()
        {
            Logger.LogTrace("StopWatcher called for {DisableBasePath}", DisableBasePath);
            if (DisableBasePath != null)
            {
                RegistryChangeMonitor.Stop();
            }
            Logger.LogTrace("Watcher stopped");
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopWatcher();
                    RegistryChangeMonitor.Changed -= ChangeHandler;
                    RegistryChangeMonitor.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Events
        public event EnableChangeHandler? Enable;
        public event EnableChangeHandler? Disable;
        #endregion
    }
}
