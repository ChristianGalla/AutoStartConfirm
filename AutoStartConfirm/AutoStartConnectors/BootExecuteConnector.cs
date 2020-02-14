namespace AutoStartConfirm.AutoStartConnectors {
    class BootExecuteConnector : RegistryConnector {
        public BootExecuteConnector() {
            EntryCategory = "Boot Execute";
            basePath = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Session Manager";
            categories = new string[] { "BootExecute", "SetupExecute", "Execute", "S0InitialCommand" };
        }
    }
}
