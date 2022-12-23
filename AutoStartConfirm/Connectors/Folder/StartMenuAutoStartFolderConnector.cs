using AutoStartConfirm.Models;
using System;

namespace AutoStartConfirm.Connectors.Folder
{
    public class StartMenuAutoStartFolderConnector : FolderConnector, IStartMenuAutoStartFolderConnector
    {

        private readonly Category category = Category.StartMenuAutoStartFolder;

        private readonly static string programmDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        private readonly static string basePath = $"{programmDataPath}\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

        private readonly static string disableBasePath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder";

        public override bool IsAdminRequiredForChanges(AutoStartEntry autoStart)
        {
            return true;
        }

        public override Category Category
        {
            get
            {
                return category;
            }
        }

        public override string BasePath
        {
            get
            {
                return basePath;
            }
        }

        public override string DisableBasePath
        {
            get
            {
                return disableBasePath;
            }
        }
    }
}
