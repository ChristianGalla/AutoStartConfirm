using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace AutoStartConfirm.Helpers
{
    public interface IDispatchService
    {
        public bool TryEnqueue(DispatcherQueueHandler callback);

        public bool TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback);

        public Task EnqueueAsync(Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal);

        public Task<T> EnqueueAsync<T>(Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal);

        public Task EnqueueAsync(Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal);

        public Task<T> EnqueueAsync<T>(Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal);
    }
}