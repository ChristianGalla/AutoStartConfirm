using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class WindowsCEServices64Connector : RegistryConnector {

        private readonly Category category = Category.WindowsCEServices64;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows CE Services";

        private readonly string[] subKeys = new string[] { "AutoStartOnConnect", "AutoStartOnDisconnect" };

        private readonly bool monitorSubkeys = true;

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
