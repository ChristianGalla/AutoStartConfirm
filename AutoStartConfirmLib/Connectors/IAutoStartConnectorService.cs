using AutoStartConfirm.Models;
using System.Collections;
using System.Collections.Generic;

namespace AutoStartConfirm.Connectors
{
    public interface IAutoStartConnectorService {
        IAutoStartConnector this[int index] { get; }

        Category Category { get; }
        Dictionary<Category, IAutoStartConnector> EnabledConnectors { get; }
        int Count { get; }

        event AutoStartChangeHandler Add;
        event AutoStartChangeHandler Disable;
        event AutoStartChangeHandler Enable;
        event AutoStartChangeHandler Remove;

        void AddAutoStart(AutoStartEntry autoStart);
        bool CanBeAdded(AutoStartEntry autoStart);
        bool CanBeDisabled(AutoStartEntry autoStart);
        bool CanBeEnabled(AutoStartEntry autoStart);
        bool CanBeRemoved(AutoStartEntry autoStart);
        void DisableAutoStart(AutoStartEntry autoStart);
        void Dispose();
        void EnableAutoStart(AutoStartEntry autoStart);
        IList<AutoStartEntry> GetCurrentAutoStarts();
        IEnumerator GetEnumerator();
        bool IsAdminRequiredForChanges(AutoStartEntry autoStart);
        bool IsEnabled(AutoStartEntry autoStart);
        void RemoveAutoStart(AutoStartEntry autoStart);
        void StartWatcher();
        void StopWatcher();
    }
}