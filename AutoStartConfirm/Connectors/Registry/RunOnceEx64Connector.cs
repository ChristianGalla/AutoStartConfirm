﻿using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry {
    class RunOnceEx64Connector : RegistryConnector {

        private readonly Category category = Category.RunOnceEx64;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = null;

        private readonly bool monitorSubkeys = true;

        public override string DisableBasePath {
            get {
                return null;
            }
        }

        public override string BasePath {
            get {
                return basePath;
            }
        }

        public override string[] SubKeyNames {
            get {
                return subKeys;
            }
        }

        public override string[] ValueNames {
            get {
                return valueNames;
            }
        }

        public override Category Category {
            get {
                return category;
            }
        }

        public override bool MonitorSubkeys {
            get {
                return monitorSubkeys;
            }
        }
    }
}
