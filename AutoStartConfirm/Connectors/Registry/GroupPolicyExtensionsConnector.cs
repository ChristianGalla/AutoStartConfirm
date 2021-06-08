using AutoStartConfirm.Models;
using Microsoft.Win32;

namespace AutoStartConfirm.Connectors.Registry {
    class GroupPolicyExtensionsConnector : RegistryConnector {

        private readonly Category category = Category.GroupPolicyExtensions;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\GPExtensions";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = null;

        protected override bool GetIsAutoStartEntry(RegistryKey currentKey, string valueName, int level) {
            return level == 1 && valueName == "DllName";
        }

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
