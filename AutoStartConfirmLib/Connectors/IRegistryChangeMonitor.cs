namespace AutoStartConfirm.Connectors {
    public interface IRegistryChangeMonitor
    {
        public string? RegistryPath { get; set; }

        bool Monitoring { get; }

        event RegistryChangeHandler? Changed;

        void Dispose();
        void Start();
        void Stop();
    }
}