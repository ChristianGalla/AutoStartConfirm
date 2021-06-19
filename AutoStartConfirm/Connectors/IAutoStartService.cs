using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AutoStartConfirm.Connectors {
    public interface IAutoStartService {
        ObservableCollection<AutoStartEntry> CurrentAutoStarts { get; }
        ObservableCollection<AutoStartEntry> HistoryAutoStarts { get; }

        event AutoStartChangeHandler Add;
        event AutoStartChangeHandler HistoryAutoStartChange;
        event AutoStartChangeHandler Confirm;
        event AutoStartChangeHandler CurrentAutoStartChange;
        event AutoStartChangeHandler Disable;
        event AutoStartChangeHandler Enable;
        event AutoStartChangeHandler Remove;

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

        /// <summary>
        /// Checks if a valid file containing auto starts of a previous run exists.
        /// </summary>
        /// <returns></returns>
        bool GetValidAutoStartFileExists();
        IList<AutoStartEntry> GetCurrentAutoStarts();
        ObservableCollection<AutoStartEntry> GetSavedCurrentAutoStarts(string path);
        bool IsAdminRequiredForChanges(AutoStartEntry autoStart);
        void LoadCanBeAdded(AutoStartEntry autoStart);
        void LoadCanBeDisabled(AutoStartEntry autoStart);
        void LoadCanBeEnabled(AutoStartEntry autoStart);
        void LoadCanBeRemoved(AutoStartEntry autoStart);
        void LoadCurrentAutoStarts();
        void RemoveAutoStart(AutoStartEntry autoStart);
        void RemoveAutoStart(Guid Id);
        void ResetEditablePropertiesOfHistoryAutoStarts(AutoStartEntry autoStart);
        void ResetEditablePropertiesOfAllHistoryAutoStarts();
        void ResetEditablePropertiesOfAllCurrentAutoStarts();
        void ResetEditablePropertiesOfAutoStarts(AutoStartEntry autoStart);
        void ResetEditablePropertiesOfCurrentAutoStarts(AutoStartEntry autoStart);
        void SaveAutoStarts();
        void StartWatcher();
        void StopWatcher();
        bool TryGetHistoryAutoStart(Guid Id, out AutoStartEntry value);
        bool TryGetCurrentAutoStart(Guid Id, out AutoStartEntry value);
    }
}