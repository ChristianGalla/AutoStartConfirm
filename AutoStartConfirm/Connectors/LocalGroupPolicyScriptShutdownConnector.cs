﻿using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class LocalGroupPolicyScriptShutdownConnector : RegistryConnector {

        private readonly Category category = Category.LocalGroupPolicyScriptShutdown;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Shutdown";

        // todo: only monitor sub sub keys script
        private readonly string[] subKeys = null;

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
