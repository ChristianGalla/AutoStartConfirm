namespace AutoStartConfirm.Connectors {
    public interface IRegistryChangeMonitor {
        bool Monitoring { get; }

        event RegistryChangeHandler Changed;

        void Dispose();
        void Start();
        void Stop();
    }
}