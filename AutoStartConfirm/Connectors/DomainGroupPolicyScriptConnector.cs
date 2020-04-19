using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class DomainGroupPolicyScriptConnector : RegistryConnector {

        private readonly Category category = Category.DomainGroupPolicyScript;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\System\Scripts";

        private readonly string[] subKeys = new string[] { "Startup", "Shutdown", "Logon", "Logoff" };

        private readonly bool monitorSubkeys = true;

        public override string BasePath {
            get {
                return basePath;
            }
        }

        public override string[] SubKeys {
            get {
                return subKeys;
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
