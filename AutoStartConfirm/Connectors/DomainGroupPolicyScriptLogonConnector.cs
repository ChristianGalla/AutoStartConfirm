using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class DomainGroupPolicyScriptLogonConnector : RegistryConnector {

        private readonly Category category = Category.DomainGroupPolicyScriptLogon;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\System\Scripts\Logon";

        // todo: only monitor sub sub keys script
        private readonly string[] subKeys = null;

        private readonly bool monitorSubkeys = true;

        public override string BasePath {
            get {
                return basePath;
            }
        }

        public override string[] ValueNames {
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
