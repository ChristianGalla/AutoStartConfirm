using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class AvailableShellsConnector : RegistryConnector {

        private readonly Category category = Category.AvailableShells;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Winlogon\AlternateShells";

        private readonly string[] subKeys = new string[] { "AvailableShells" };

        private readonly bool monitorSubkeys = false;

        public override string BasePath {
            get {
                return basePath;
            }
        }

        public override string[] SubKeys {
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
