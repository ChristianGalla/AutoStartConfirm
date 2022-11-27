using Microsoft.Win32;
using System;

namespace AutoStartConfirm.Models
{
    [Serializable]
    public class RegistryAutoStartEntry: AutoStartEntry {
        public RegistryValueKind RegistryValueKind { get; set; }
    }
}
