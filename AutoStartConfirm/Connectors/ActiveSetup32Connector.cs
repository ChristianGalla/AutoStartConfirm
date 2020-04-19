using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class ActiveSetup32Connector : RegistryConnector {

        private readonly Category category = Category.ActiveSetup32;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components";

        private readonly string[] subKeys = null;

        private readonly bool monitorSubkeys = true; // todo: filter for StubPath

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
