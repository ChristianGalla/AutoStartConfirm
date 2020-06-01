using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class TerminalServerStartupProgramsConnector : RegistryConnector {

        private readonly Category category = Category.TerminalServerStartupPrograms;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Terminal Server\Wds\rdpwd";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = new string[] { "StartupPrograms" };

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
