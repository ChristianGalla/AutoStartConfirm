using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Folder
{
    public interface ICurrentUserStartMenuAutoStartFolderConnector: IAutoStartConnector
    {
        string BasePath { get; }
        string DisableBasePath { get; }
    }
}