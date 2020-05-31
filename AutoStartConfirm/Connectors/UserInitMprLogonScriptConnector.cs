using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class UserInitMprLogonScriptConnector : RegistryConnector {

        private readonly Category category = Category.UserInitMprLogonScript;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Environment";

        private readonly string[] subKeys = new string[] { "UserInitMprLogonScript" };

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
