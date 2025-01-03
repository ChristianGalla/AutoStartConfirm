using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace AutoStartConfirm.Helpers
{
    public class DispatchService: IDispatchService
    {
        private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private DispatcherQueue DispatcherQueue
        {
            get
            {
                return dispatcherQueue;
            }
        }

        public bool TryEnqueue(DispatcherQueueHandler callback)
        {
            return DispatcherQueue.TryEnqueue(callback);
        }

        public bool TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
        {
            return DispatcherQueue.TryEnqueue(priority, callback);
        }

        public async Task EnqueueAsync(Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            await DispatcherQueue.EnqueueAsync(function, priority);
        }

        public async Task<T> EnqueueAsync<T>(Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            return await DispatcherQueue.EnqueueAsync(function, priority);
        }

        public async Task EnqueueAsync(Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            await DispatcherQueue.EnqueueAsync(function, priority);
        }

        public async Task<T> EnqueueAsync<T>(Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
        {
            return await DispatcherQueue.EnqueueAsync(function, priority);
        }
    }
}
