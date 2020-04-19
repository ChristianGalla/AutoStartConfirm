using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class TerminalServerStartupProgramsConnector : RegistryConnector {

        private readonly Category category = Category.TerminalServerStartupPrograms;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Terminal Server\Wds\rdpwd";

        private readonly string[] subKeys = new string[] { "StartupPrograms" };

        private readonly bool monitorSubkeys = false;

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
