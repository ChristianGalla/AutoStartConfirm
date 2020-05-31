using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class CurrentUserGroupPolicyShellOverwriteConnector : RegistryConnector {

        private readonly Category category = Category.CurrentUserGroupPolicyShellOverwrite;

        private readonly string basePath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\System";

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
