using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry
{
    public interface ILocalGroupPolicyScriptStartupConnector : IAutoStartConnector, IRegistryConnector
    {
    }
}