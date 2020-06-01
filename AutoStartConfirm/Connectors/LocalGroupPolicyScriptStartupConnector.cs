using AutoStartConfirm.AutoStarts;
using Microsoft.Win32;

namespace AutoStartConfirm.Connectors {
    class LocalGroupPolicyScriptStartupConnector : RegistryConnector {

        private readonly Category category = Category.LocalGroupPolicyScriptStartup;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Startup";

        protected override bool GetIsAutoStartEntry(RegistryKey currentKey, string valueName, int level) {
            return level == 2 && valueName == "script";
        }

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = null;

        private readonly bool monitorSubkeys = true;

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
