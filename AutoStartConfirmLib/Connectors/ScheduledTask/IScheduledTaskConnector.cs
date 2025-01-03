using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.ScheduledTask
{
    public interface IScheduledTaskConnector: IAutoStartConnector
    {
        void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false);
    }
}