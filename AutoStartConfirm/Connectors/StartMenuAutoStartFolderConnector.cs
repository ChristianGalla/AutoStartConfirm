using AutoStartConfirm.AutoStarts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {
    class StartMenuAutoStartFolderConnector : FolderConnector {

        private readonly Category category = Category.StartMenuAutoStartFolder;

        private readonly static string programmDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        private readonly static string basePath = $"{programmDataPath}\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

        public override bool IsAdminRequiredForChanges {
            get {
                return true;
            }
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
