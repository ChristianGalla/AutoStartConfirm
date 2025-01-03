using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.Registry
{
    public class WindowsCEServicesAutoStartOnDisconnect64Connector : RegistryConnector, IWindowsCEServicesAutoStartOnDisconnect64Connector
    {

        private readonly Category category = Category.WindowsCEServicesAutoStartOnDisconnect64;

        public const string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows CE Services\AutoStartOnDisconnect";

        public const string[]? subKeys = null;

        public const string[]? valueNames = null;

        private readonly bool monitorSubkeys = true;

        public WindowsCEServicesAutoStartOnDisconnect64Connector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
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
