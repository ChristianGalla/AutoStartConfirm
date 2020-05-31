﻿using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class GroupPolicyRunConnector : RegistryConnector {

        private readonly Category category = Category.GroupPolicyRun;

        private readonly string basePath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run";

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
