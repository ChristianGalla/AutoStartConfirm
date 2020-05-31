using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class DomainGroupPolicyScriptStartupConnector : RegistryConnector {

        private readonly Category category = Category.DomainGroupPolicyScriptStartup;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\System\Scripts\Startup";

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
