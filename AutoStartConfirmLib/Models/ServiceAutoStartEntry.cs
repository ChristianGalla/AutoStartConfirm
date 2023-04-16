using System;
using System.ServiceProcess;

namespace AutoStartConfirm.Models
{
    [Serializable]
    public class ServiceAutoStartEntry: AutoStartEntry {
        public ServiceStartMode EnabledStartMode;
    }
}
