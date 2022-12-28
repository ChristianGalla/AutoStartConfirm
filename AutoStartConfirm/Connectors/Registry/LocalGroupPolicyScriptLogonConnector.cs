using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AutoStartConfirm.Connectors.Registry {
    public class LocalGroupPolicyScriptLogonConnector : RegistryConnector, ILocalGroupPolicyScriptLogonConnector
    {

        private readonly Category category = Category.LocalGroupPolicyScriptLogon;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Logon";

        protected override bool GetIsAutoStartEntry(RegistryKey currentKey, string valueName, int level)
        {
            return level == 2 && valueName == "script";
        }

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = null;

        private readonly bool monitorSubkeys = true;

        public LocalGroupPolicyScriptLogonConnector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
        {
        }

        public override string DisableBasePath
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

        public override string[] SubKeyNames
        {
            get
            {
                return subKeys;
            }
        }

        public override string[] ValueNames
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
