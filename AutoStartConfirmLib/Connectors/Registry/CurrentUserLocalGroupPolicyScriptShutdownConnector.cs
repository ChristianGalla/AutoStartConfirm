using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AutoStartConfirm.Connectors.Registry {
    public class CurrentUserLocalGroupPolicyScriptShutdownConnector : RegistryConnector, ICurrentUserLocalGroupPolicyScriptShutdownConnector
    {

        private readonly Category category = Category.CurrentUserLocalGroupPolicyScriptShutdown;

        public const string basePath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Shutdown";

        protected override bool GetIsAutoStartEntry(RegistryKey currentKey, string valueName, int level)
        {
            return level == 2 && valueName == "script";
        }

        public const string[]? subKeys = null;

        public const string[]? valueNames = null;

        private readonly bool monitorSubkeys = true;

        public CurrentUserLocalGroupPolicyScriptShutdownConnector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
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
