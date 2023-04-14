using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry
{
    public interface IAlternateShellConnector : IAutoStartConnector, IRegistryConnector
    {
    }
}