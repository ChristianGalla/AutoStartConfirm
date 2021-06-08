using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry {
    class GroupPolicyShellOverwriteConnector : RegistryConnector {

        private readonly Category category = Category.GroupPolicyShellOverwrite;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = new string[] { "Shell" };

        private readonly bool monitorSubkeys = false;

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
