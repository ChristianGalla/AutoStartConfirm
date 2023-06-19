using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AutoStartConfirm.Business {
    public interface IAutoStartBusiness: IDisposable {
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

        /// <summary>
        /// All ignored auto starts
        /// </summary>
        ObservableCollection<IgnoredAutoStart> IgnoredAutoStarts { get; }

        string RevertAddParameterName { get; }

        string RevertRemoveParameterName { get; }

        string EnableParameterName { get; }

        string DisableParameterName { get; }

        bool HasOwnAutoStart { get; }
        string CurrentExePath { get; set; }

        bool CanAutoStartBeAdded(AutoStartEntry autoStart);
        bool CanAutoStartBeDisabled(AutoStartEntry autoStart);
        bool CanAutoStartBeEnabled(AutoStartEntry autoStart);
        bool CanAutoStartBeRemoved(AutoStartEntry autoStart);
        bool CanAutoStartBeIgnored(AutoStartEntry autoStart);

        #region AutoStart changes
        Task AddAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true);
        Task AddAutoStart(Guid Id, bool showDialogsAndCatchErrors = true);
        Task ConfirmAdd(AutoStartEntry autoStart);
        Task ConfirmAdd(Guid Id);
        Task ConfirmRemove(AutoStartEntry autoStart);
        Task ConfirmRemove(Guid Id);
        Task IgnoreAutoStart(AutoStartEntry autoStart);
        Task IgnoreAutoStart(Guid Id);
        Task RemoveIgnoreAutoStart(IgnoredAutoStart autoStart);
        Task DisableAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true);
        Task DisableAutoStart(Guid Id, bool showDialogsAndCatchErrors = true);
        Task EnableAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true);
        Task EnableAutoStart(Guid Id, bool showDialogsAndCatchErrors = true);
        Task RemoveAutoStart(AutoStartEntry autoStart, bool showDialogsAndCatchErrors = true);
        Task RemoveAutoStart(Guid Id, bool showDialogsAndCatchErrors = true);
        Task ToggleOwnAutoStart(bool showDialogsAndCatchErrors = true);
        #endregion

        /// <summary>
        /// Checks if a valid file containing auto starts of a previous run exists.
        /// </summary>
        /// <returns></returns>
        bool GetValidAutoStartFileExists();
        IList<AutoStartEntry> GetCurrentAutoStarts();
        ObservableCollection<T>? GetSavedAutoStarts<T>(string path);
        bool IsAdminRequiredForChanges(AutoStartEntry autoStart);
        Task<bool> LoadCanBeAdded(AutoStartEntry autoStart);
        Task<bool> LoadCanBeDisabled(AutoStartEntry autoStart);
        Task<bool> LoadCanBeEnabled(AutoStartEntry autoStart);
        Task<bool> LoadCanBeRemoved(AutoStartEntry autoStart);
        Task<bool> LoadCanBeIgnored(AutoStartEntry autoStart);
        void LoadCurrentAutoStarts();
        void ResetEditablePropertiesOfHistoryAutoStarts(AutoStartEntry autoStart);
        void ResetEditablePropertiesOfAllHistoryAutoStarts();
        void ResetEditablePropertiesOfAllCurrentAutoStarts();
        void ResetEditablePropertiesOfAutoStarts(AutoStartEntry autoStart);
        void ResetEditablePropertiesOfCurrentAutoStarts(AutoStartEntry autoStart);
        void SaveAutoStarts();
        void StartWatcher();
        void StopWatcher();
        bool TryGetHistoryAutoStart(Guid Id, [NotNullWhen(returnValue: true)] out AutoStartEntry? value);
        bool TryGetCurrentAutoStart(Guid Id, [NotNullWhen(returnValue: true)] out AutoStartEntry? value);
        bool IsOwnAutoStart(AutoStartEntry autoStart);
        Task ClearHistory();
        void Run();
    }
}