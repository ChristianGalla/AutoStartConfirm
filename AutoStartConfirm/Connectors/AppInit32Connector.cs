using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class AppInit32Connector : RegistryConnector {
        public AppInit32Connector() {
            Category = Category.AppInit32;
            basePath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows NT\\CurrentVersion\\Windows";
            categories = new string[] { "Appinit_Dlls" };
        }
    }
}
