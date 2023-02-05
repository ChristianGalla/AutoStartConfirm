using Microsoft.UI.Dispatching;

namespace AutoStartConfirm.Helpers
{
    public interface IDispatchService
    {
        DispatcherQueue DispatcherQueue { get; }
    }
}