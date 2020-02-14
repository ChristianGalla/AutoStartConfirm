using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.AutoStartConnectors
{
    interface IAutoStartConnector: IDisposable
    {
        IEnumerable<AutoStartEntry> GetCurrentAutoStarts();

        void StartWartcher();

        void StopWartcher();
    }
}
