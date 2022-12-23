using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry
{
    public interface IWindowsCEServicesAutoStartOnDisconnect32Connector : IAutoStartConnector
    {
        string BasePath { get; }
        string DisableBasePath { get; }
        bool MonitorSubkeys { get; }
        string[] SubKeyNames { get; }
        string[] ValueNames { get; }
    }
}