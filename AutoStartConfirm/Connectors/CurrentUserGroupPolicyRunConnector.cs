using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class CurrentUserGroupPolicyRunConnector : RegistryConnector {

        private readonly Category category = Category.CurrentUserGroupPolicyRun;

        private readonly string basePath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run";

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
