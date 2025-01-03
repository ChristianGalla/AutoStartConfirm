﻿using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.Registry
{
    public class AppCertDllConnector : RegistryConnector, IAppCertDllConnector
    {

        private readonly Category category = Category.AppCertDll;

        public const string basePath = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\AppCertDlls";

        public const string[]? subKeys = null;

        public const string[]? valueNames = null;

        private readonly bool monitorSubkeys = true;

        public AppCertDllConnector(ILogger<RegistryConnector> logger, IRegistryDisableService registryDisableService, IRegistryChangeMonitor registryChangeMonitor) : base(logger, registryDisableService, registryChangeMonitor)
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
