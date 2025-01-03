namespace AutoStartConfirm.Connectors.Registry
{
    public interface IRegistryConnector : IAutoStartConnector
    {
        public string BasePath { get; }

        public string? DisableBasePath { get; }

        public bool MonitorSubkeys { get; }

        public string[]? SubKeyNames { get; }

        public string[]? ValueNames { get; }

    }
}