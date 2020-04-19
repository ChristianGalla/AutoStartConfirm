using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class WindowsCEServices32Connector : RegistryConnector {

        private readonly Category category = Category.WindowsCEServices32;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows CE Services";

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
