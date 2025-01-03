using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using System;

namespace AutoStartConfirm.Connectors.Folder
{
    public class CurrentUserStartMenuAutoStartFolderConnector : FolderConnector, ICurrentUserStartMenuAutoStartFolderConnector
    {

        private readonly Category category = Category.CurrentUserStartMenuAutoStartFolder;

        private readonly static string programmDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private readonly static string basePath = $"{programmDataPath}\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";

        private const string disableBasePath = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder";

        public CurrentUserStartMenuAutoStartFolderConnector(
            ILogger<FolderConnector> logger,
            IRegistryDisableService registryDisableService,
            IFolderChangeMonitor folderChangeMonitor
        ) : base(logger, registryDisableService, folderChangeMonitor)
        {
        }

        public override string DisableBasePath
        {
            get
            {
                return disableBasePath;
            }
        }

        public override bool IsAdminRequiredForChanges(AutoStartEntry autoStart)
        {
            return false;
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
    }
}
