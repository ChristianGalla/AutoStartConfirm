namespace AutoStartConfirm.Connectors {
    public interface IRegistryChangeMonitor {
        bool Monitoring { get; }

        event RegistryChangeHandler Changed;
        event RegistryChangeHandler Error;

        void Dispose();
        void Start();
        void Stop();
    }
}