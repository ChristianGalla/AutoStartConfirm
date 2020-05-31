using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class AppCertDllConnector : RegistryConnector {

        private readonly Category category = Category.AppCertDll;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\AppCertDlls";

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
