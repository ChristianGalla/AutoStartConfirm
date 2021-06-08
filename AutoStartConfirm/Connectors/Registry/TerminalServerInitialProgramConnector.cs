using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry {
    class TerminalServerInitialProgramConnector : RegistryConnector {

        private readonly Category category = Category.TerminalServerInitialProgram;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = new string[] { "InitialProgram" };

        private readonly bool monitorSubkeys = false;

        public override string DisableBasePath {
            get {
                return null;
            }
        }

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
