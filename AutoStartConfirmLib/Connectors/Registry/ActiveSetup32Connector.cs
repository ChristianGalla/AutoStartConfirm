﻿using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AutoStartConfirm.Connectors.Registry
{
    public class ActiveSetup32Connector : RegistryConnector, IActiveSetup32Connector
    {

        private readonly Category category = Category.ActiveSetup32;

        public const string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components";

        public const string[]? subKeys = null;

        public const string[]? valueNames = null;

        public override string? DisableBasePath
        {
            get
            {
                return null;
            }
        }

        protected override bool GetIsAutoStartEntry(RegistryKey currentKey, string valueName, int level)
        {
            return level == 1 && valueName == "StubPath";
        }

        private readonly bool monitorSubkeys = true;

        public ActiveSetup32Connector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
        {
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
