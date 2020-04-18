using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class AppInit64Connector : RegistryConnector {
        public AppInit64Connector() {
            Category = Category.AppInit64;
            basePath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows";
            categories = new string[] { "Appinit_Dlls" };
        }
    }
}
