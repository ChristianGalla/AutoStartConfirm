﻿using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class LocalGroupPolicyScriptLogoffConnector : RegistryConnector {

        private readonly Category category = Category.LocalGroupPolicyScriptLogoff;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Logoff";

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
