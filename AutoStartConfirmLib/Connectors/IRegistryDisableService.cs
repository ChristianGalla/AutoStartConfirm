using AutoStartConfirm.Models;
using System.Collections.Generic;

namespace AutoStartConfirm.Connectors {
    public interface IRegistryDisableService {
        string? DisableBasePath { get; set; }

        event EnableChangeHandler? Disable;
        event EnableChangeHandler? Enable;

        bool CanBeDisabled(AutoStartEntry autoStart);
        bool CanBeEnabled(AutoStartEntry autoStart);
        void DisableAutoStart(AutoStartEntry autoStart);
        void DisableAutoStart(AutoStartEntry autoStart, bool dryRun);
        void Dispose();
        void EnableAutoStart(AutoStartEntry autoStart);
        void EnableAutoStart(AutoStartEntry autoStart, bool dryRun);
        Dictionary<string, bool> GetCurrentEnableStatus();
        void StartWatcher();
        void StopWatcher();
        void ToggleAutoStartEnable(AutoStartEntry autoStart, bool enable, bool dryRun);
    }
}