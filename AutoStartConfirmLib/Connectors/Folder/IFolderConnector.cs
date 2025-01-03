using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Folder
{
    public interface IFolderConnector: IAutoStartConnector
    {
        public string BasePath { get; }

        public string DisableBasePath { get; }

        void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false);
    }
}