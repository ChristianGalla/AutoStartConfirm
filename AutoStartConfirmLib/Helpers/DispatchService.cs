using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Helpers
{
    public class DispatchService: IDispatchService
    {
        private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public DispatcherQueue DispatcherQueue
        {
            get
            {
                return dispatcherQueue;
            }
        }
    }
}
