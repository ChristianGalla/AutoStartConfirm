using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class CurrentUserUserInitMprLogonScriptConnector : RegistryConnector {

        private readonly Category category = Category.CurrentUserUserInitMprLogonScript;

        private readonly string basePath = @"HKEY_CURRENT_USER\Environment";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = new string[] { "UserInitMprLogonScript" };

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
