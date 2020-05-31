using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class DomainGroupPolicyScriptLogoffConnector : RegistryConnector {

        private readonly Category category = Category.DomainGroupPolicyScriptLogoff;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\System\Scripts\Logoff";

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
