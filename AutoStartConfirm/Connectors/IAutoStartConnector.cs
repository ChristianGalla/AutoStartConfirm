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
        IEnumerable<AutoStartEntry> GetCurrentAutoStarts();

        #region Watcher
        void StartWatcher();
        void StopWatcher();
        #endregion

        #region Events
        event AddHandler Add;
        event RemoveHandler Remove;
        #endregion
    }
}
