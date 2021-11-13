using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors.Folder {
    public class StartMenuAutoStartFolderConnector : FolderConnector {

        private readonly Category category = Category.StartMenuAutoStartFolder;

        private readonly static string programmDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        private readonly static string basePath = $"{programmDataPath}\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

        private readonly static string disableBasePath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder";

        public override bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            return true;
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

        public override string DisableBasePath {
            get {
                return disableBasePath;
            }
        }
    }
}
