using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry
{
    public interface ILogonConnector : IAutoStartConnector, IRegistryConnector
    {
    }
}