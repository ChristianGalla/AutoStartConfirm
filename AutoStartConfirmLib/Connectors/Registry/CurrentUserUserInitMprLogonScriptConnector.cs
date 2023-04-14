using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.Registry {
    public class CurrentUserUserInitMprLogonScriptConnector : RegistryConnector, ICurrentUserUserInitMprLogonScriptConnector
    {

        private readonly Category category = Category.CurrentUserUserInitMprLogonScript;

        public const string basePath = @"HKEY_CURRENT_USER\Environment";

        public const string[]? subKeys = null;

        private readonly string[] valueNames = new string[] { "UserInitMprLogonScript" };

        private readonly bool monitorSubkeys = false;

        public CurrentUserUserInitMprLogonScriptConnector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
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
