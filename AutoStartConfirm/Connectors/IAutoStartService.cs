using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {
    public interface IAutoStartService: IDisposable {
        /// <summary>
        /// All current auto starts of all enabled connectors
        /// </summary>
        ObservableCollection<AutoStartEntry> CurrentAutoStarts { get; }

        /// <summary>
        /// All current auto starts of all connectors
        /// </summary>
        ObservableCollection<AutoStartEntry> AllCurrentAutoStarts { get; }

        /// <summary>
        /// All history auto starts of all enabled connectors
        /// </summary>
        ObservableCollection<AutoStartEntry> HistoryAutoStarts { get; }

        /// <summary>
        /// All history auto starts of all connectors
        /// </summary>
        ObservableCollection<AutoStartEntry> AllHistoryAutoStarts { get; }

        bool HasOwnAutoStart { get; }
        string CurrentExePath { get; set; }

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
        void ConfirmAdd(AutoStartEntry autoStart);
        void ConfirmAdd(Guid Id);
        void ConfirmRemove(AutoStartEntry autoStart);
        void ConfirmRemove(Guid Id);
        void DisableAutoStart(AutoStartEntry autoStart);
        void DisableAutoStart(Guid Id);
        void EnableAutoStart(AutoStartEntry autoStart);
        void EnableAutoStart(Guid Id);

        /// <summary>
        /// Checks if a valid file containing auto starts of a previous run exists.
        /// </summary>
        /// <returns></returns>
        bool GetValidAutoStartFileExists();
        IList<AutoStartEntry> GetCurrentAutoStarts();
        ObservableCollection<AutoStartEntry> GetSavedAutoStarts(string path);
        bool IsAdminRequiredForChanges(AutoStartEntry autoStart);
        Task<bool> LoadCanBeAdded(AutoStartEntry autoStart);
        Task<bool> LoadCanBeDisabled(AutoStartEntry autoStart);
        Task<bool> LoadCanBeEnabled(AutoStartEntry autoStart);
        Task<bool> LoadCanBeRemoved(AutoStartEntry autoStart);
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
        bool IsOwnAutoStart(AutoStartEntry autoStart);
        void ToggleOwnAutoStart();
    }
}