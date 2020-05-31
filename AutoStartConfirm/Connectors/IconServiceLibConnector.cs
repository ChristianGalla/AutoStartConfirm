using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class IconServiceLibConnector : RegistryConnector {

        private readonly Category category = Category.IconServiceLib;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Windows";

        private readonly string[] subKeys = new string[] { "IconServiceLib" };

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
