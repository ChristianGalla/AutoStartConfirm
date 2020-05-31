using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class AppInit64Connector : RegistryConnector {

        private readonly Category category = Category.AppInit64;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows";

        private readonly string[] subKeys = new string[] { "Appinit_Dlls" };

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
