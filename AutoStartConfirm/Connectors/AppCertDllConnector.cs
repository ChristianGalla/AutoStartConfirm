using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    class AppCertDllConnector : RegistryConnector {
        public AppCertDllConnector() {
            Category = Category.AppCertDll;
            basePath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager\\AppCertDlls";
            categories = null;
        }
    }
}
