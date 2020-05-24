﻿using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class CurrentUserGroupPolicyRunConnector : RegistryConnector {

        private readonly Category category = Category.CurrentUserGroupPolicyRun;

        private readonly string basePath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run";

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
