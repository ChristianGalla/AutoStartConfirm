using AutoStartConfirm.AutoStarts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {

    #region Delegates
    public delegate void AutoStartChangeHandler(AutoStartEntry e);
    #endregion

    public interface IAutoStartConnector : IDisposable {
        #region Fields
        Category Category { get; }

        bool IsAdminRequiredForChanges { get; }
        #endregion

        #region Methods

        IList<AutoStartEntry> GetCurrentAutoStarts();

        bool CanBeAdded(AutoStartEntry autoStart);
        bool CanBeRemoved(AutoStartEntry autoStart);

        void AddAutoStart(AutoStartEntry autoStart);
        void RemoveAutoStart(AutoStartEntry autoStart);


        bool CanBeEnabled(AutoStartEntry autoStart);
        bool CanBeDisabled(AutoStartEntry autoStart);

        void EnableAutoStart(AutoStartEntry autoStart);
        void DisableAutoStart(AutoStartEntry autoStart);

        #region Watcher
        void StartWatcher();
        void StopWatcher();
        #endregion
        #endregion

        #region Events
        event AutoStartChangeHandler Add;
        event AutoStartChangeHandler Remove;
        event AutoStartChangeHandler Enable;
        event AutoStartChangeHandler Disable;

        #endregion
    }
}
