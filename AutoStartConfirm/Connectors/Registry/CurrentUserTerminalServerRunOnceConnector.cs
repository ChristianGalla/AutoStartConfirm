using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry {
    public class CurrentUserTerminalServerRunOnceConnector : RegistryConnector, ICurrentUserTerminalServerRunOnceConnector
    {

        private readonly Category category = Category.CurrentUserTerminalServerRunOnce;

        private readonly string basePath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Runonce";

        private readonly string[] subKeys = null;

        private readonly string[] valueNames = null;

        private readonly bool monitorSubkeys = true;

        public override string DisableBasePath
        {
            get
            {
                return null;
            }
        }

        public override string BasePath
        {
            get
            {
                return basePath;
            }
        }

        public override string[] SubKeyNames
        {
            get
            {
                return subKeys;
            }
        }

        public override string[] ValueNames
        {
            get
            {
                return valueNames;
            }
        }

        public override Category Category
        {
            get
            {
                return category;
            }
        }

        public override bool MonitorSubkeys
        {
            get
            {
                return monitorSubkeys;
            }
        }
    }
}
