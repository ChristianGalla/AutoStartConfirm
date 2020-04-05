using AutoStartConfirm.AutoStarts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {

    #region Delegates
    delegate void AddHandler(AutoStartEntry e);
    delegate void RemoveHandler(AutoStartEntry e);
    #endregion

    interface IAutoStartConnector : IDisposable
    {
        #region Methods
        IEnumerable<AutoStartEntry> GetCurrentAutoStarts();

        void AddAutoStart(AutoStartEntry autoStart);
        void RemoveAutoStart(AutoStartEntry autoStart);

        #region Watcher
        void StartWatcher();
        void StopWatcher();
        #endregion
        #endregion

        #region Events
        event AddHandler Add;
        event RemoveHandler Remove;
        #endregion
    }
}
