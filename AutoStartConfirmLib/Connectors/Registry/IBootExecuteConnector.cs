using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry
{
    public interface IBootExecuteConnector : IAutoStartConnector, IRegistryConnector
    {
    }
}