using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class TerminalServerInitialProgramConnector : RegistryConnector {

        private readonly Category category = Category.TerminalServerInitialProgram;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";

        private readonly string[] subKeys = new string[] { "InitialProgram" };

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
