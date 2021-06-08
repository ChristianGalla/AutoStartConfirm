using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors.Folder {
    class CurrentUserStartMenuAutoStartFolderConnector : FolderConnector {

        private readonly Category category = Category.CurrentUserStartMenuAutoStartFolder;

        private readonly static string programmDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private readonly static string basePath = $"{programmDataPath}\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

        private readonly static string disableBasePath = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder";

        public override string DisableBasePath {
            get {
                return disableBasePath;
            }
        }

        public override bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            return false;
        }

        public override Category Category {
            get {
                return category;
            }
        }

        public override string BasePath {
            get {
                return basePath;
            }
        }
    }
}
