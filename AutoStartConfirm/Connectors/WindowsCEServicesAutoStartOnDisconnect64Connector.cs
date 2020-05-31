using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class WindowsCEServicesAutoStartOnDisconnect64Connector : RegistryConnector {

        private readonly Category category = Category.WindowsCEServicesAutoStartOnDisconnect64;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows CE Services\AutoStartOnDisconnect";

        private readonly string[] subKeys = null;

        private readonly bool monitorSubkeys = true;

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
