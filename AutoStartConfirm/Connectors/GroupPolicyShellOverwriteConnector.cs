using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class GroupPolicyShellOverwriteConnector : RegistryConnector {

        private readonly Category category = Category.GroupPolicyShellOverwrite;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System";

        private readonly string[] subKeys = new string[] { "Shell" };

        private readonly bool monitorSubkeys = false;

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
