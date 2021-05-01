using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class LogonConnector : RegistryConnector {

        private readonly Category category = Category.Winlogon;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = new string[] { "VmApplet", "Userinit", "Shell", "TaskMan", "AppSetup" };

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
