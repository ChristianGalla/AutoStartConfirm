using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AutoStartConfirm.AutoStarts
{
    [Serializable]
    public class RegistryAutoStartEntry: AutoStartEntry {
        public RegistryValueKind RegistryValueKind { get; set; }
    }
}
