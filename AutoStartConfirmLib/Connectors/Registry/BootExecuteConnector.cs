using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.Registry
{
    public class BootExecuteConnector : RegistryConnector, IBootExecuteConnector
    {

        private readonly Category category = Category.BootExecute;

        public const string basePath = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager";

        public const string[]? subKeys = null;

        private readonly string[] valueNames = new string[] { "BootExecute", "SetupExecute", "Execute", "S0InitialCommand" };

        private readonly bool monitorSubkeys = false;

        public BootExecuteConnector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
        {
        }

        public override string? DisableBasePath
        {
            get
            {
                return null;
            }
        }

        public override string BasePath
        {
            get
            {
                return basePath;
            }
        }

        public override string[]? SubKeyNames
        {
            get
            {
                return subKeys;
            }
        }

        public override string[]? ValueNames
        {
            get
            {
                return valueNames;
            }
        }

        public override Category Category
        {
            get
            {
                return category;
            }
        }

        public override bool MonitorSubkeys
        {
            get
            {
                return monitorSubkeys;
            }
        }
    }
}
