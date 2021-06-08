using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AutoStartConfirm.Models
{
    [Serializable]
    public class RegistryAutoStartEntry: AutoStartEntry {
        public RegistryValueKind RegistryValueKind { get; set; }
    }
}
