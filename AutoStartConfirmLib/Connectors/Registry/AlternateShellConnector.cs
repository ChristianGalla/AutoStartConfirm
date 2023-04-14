using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.Registry
{
    public class AlternateShellConnector : RegistryConnector, IAlternateShellConnector
    {

        private readonly Category category = Category.AlternateShell;

        public const string basePath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot";

        public const string[]? subKeys = null;

        private readonly string[] valueNames = new string[] { "AlternateShell" };

        private readonly bool monitorSubkeys = false;

        public AlternateShellConnector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
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
