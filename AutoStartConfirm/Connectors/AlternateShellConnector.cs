using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class AlternateShellConnector : RegistryConnector {

        private readonly Category category = Category.AlternateShell;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SafeBoot";

        private readonly string[] subKeys = new string[] { "AlternateShell" };

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
