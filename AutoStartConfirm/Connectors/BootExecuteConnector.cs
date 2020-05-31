using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class BootExecuteConnector : RegistryConnector {

        private readonly Category category = Category.BootExecute;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager";

        private readonly string[] subKeys = new string[] { "BootExecute", "SetupExecute", "Execute", "S0InitialCommand",
#if DEBUG
            "Test"
#endif
        };

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
