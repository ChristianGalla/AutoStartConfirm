using AutoStartConfirm.Models;
using System.Collections.Generic;

namespace AutoStartConfirm.Connectors.Folder
{
    public interface IFolderConnector: IAutoStartConnector
    {
        string BasePath { get; }
        string DisableBasePath { get; }
        void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false);
    }
}