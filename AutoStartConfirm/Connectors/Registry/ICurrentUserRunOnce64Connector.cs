﻿using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Registry
{
    public interface ICurrentUserRunOnce64Connector : IAutoStartConnector
    {
        string BasePath { get; }
        string DisableBasePath { get; }
        bool MonitorSubkeys { get; }
        string[] SubKeyNames { get; }
        string[] ValueNames { get; }
    }
}