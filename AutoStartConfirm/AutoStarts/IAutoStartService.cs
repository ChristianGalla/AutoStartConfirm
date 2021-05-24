using AutoStartConfirm.Connectors;
using System;
using System.Collections.Generic;

namespace AutoStartConfirm.AutoStarts {
    public interface IAutoStartService {
        Dictionary<Guid, AutoStartEntry> CurrentAutoStarts { get; }
        Dictionary<Guid, AutoStartEntry> AddedAutoStarts { get; }
        Dictionary<Guid, AutoStartEntry> RemovedAutoStarts { get; }

        event AutoStartChangeHandler Add;
        event AutoStartChangeHandler AddAutoStartChange;
        event AutoStartChangeHandler Confirm;
        event AutoStartChangeHandler CurrentAutoStartChange;
        event AutoStartChangeHandler Disable;
        event AutoStartChangeHandler Enable;
        event AutoStartChangeHandler Remove;
        event AutoStartChangeHandler RemoveAutoStartChange;

        void AddAutoStart(AutoStartEntry autoStart);
        void AddAutoStart(Guid Id);
        bool CanAutoStartBeAdded(AutoStartEntry autoStart);
        bool CanAutoStartBeDisabled(AutoStartEntry autoStart);
        bool CanAutoStartBeEnabled(AutoStartEntry autoStart);
        bool CanAutoStartBeRemoved(AutoStartEntry autoStart);
        void ConfirmAdd(Guid Id);
        void ConfirmRemove(Guid Id);
        void DisableAutoStart(AutoStartEntry autoStart);
        void DisableAutoStart(Guid Id);
        void Dispose();
        void EnableAutoStart(AutoStartEntry autoStart);
        void EnableAutoStart(Guid Id);
        bool GetAutoStartFileExists();
        IList<AutoStartEntry> GetCurrentAutoStarts();
        Dictionary<Guid, AutoStartEntry> GetSavedAutoStarts(string path);
        bool IsAdminRequiredForChanges(AutoStartEntry autoStart);
        void LoadCanBeAdded(AutoStartEntry autoStart);
        void LoadCanBeDisabled(AutoStartEntry autoStart);
        void LoadCanBeEnabled(AutoStartEntry autoStart);
        void LoadCanBeRemoved(AutoStartEntry autoStart);
        void LoadCurrentAutoStarts();
        void RemoveAutoStart(AutoStartEntry autoStart);
        void RemoveAutoStart(Guid Id);
        void ResetEditablePropertiesOfAddedAutoStarts(AutoStartEntry autoStart);
        void ResetEditablePropertiesOfAllAddedAutoStarts();
        void ResetEditablePropertiesOfAllCurrentAutoStarts();
        void ResetEditablePropertiesOfAllRemovedAutoStarts();
        void ResetEditablePropertiesOfAutoStarts(AutoStartEntry autoStart);
        void ResetEditablePropertiesOfCurrentAutoStarts(AutoStartEntry autoStart);
        void ResetEditablePropertiesOfRemovedAutoStarts(AutoStartEntry autoStart);
        void SaveAutoStarts();
        void StartWatcher();
        void StopWatcher();
        bool TryGetAddedAutoStart(Guid Id, out AutoStartEntry value);
        bool TryGetCurrentAutoStart(Guid Id, out AutoStartEntry value);
        bool TryGetRemovedAutoStart(Guid Id, out AutoStartEntry value);
    }
}