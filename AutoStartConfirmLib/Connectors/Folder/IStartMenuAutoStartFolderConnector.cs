using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Folder
{
    public interface IStartMenuAutoStartFolderConnector: IAutoStartConnector
    {
        string BasePath { get; }
        string DisableBasePath { get; }
    }
}