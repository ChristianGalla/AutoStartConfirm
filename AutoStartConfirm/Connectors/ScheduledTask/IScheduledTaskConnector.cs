using AutoStartConfirm.Models;
using System.Collections.Generic;

namespace AutoStartConfirm.Connectors.ScheduledTask
{
    public interface IScheduledTaskConnector: IAutoStartConnector
    {
        void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false);
    }
}