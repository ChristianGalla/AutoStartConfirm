using AutoStartConfirm.AutoStarts;
using Microsoft.Win32;

namespace AutoStartConfirm.Connectors {
    class AppInit32Connector : RegistryConnector {

        private readonly Category category = Category.AppInit32;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Windows";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = new string[] { "Appinit_Dlls" };

        private readonly bool monitorSubkeys = false;

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
