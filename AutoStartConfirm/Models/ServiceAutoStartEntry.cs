﻿using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceProcess;

namespace AutoStartConfirm.Models
{
    [Serializable]
    public class ServiceAutoStartEntry: AutoStartEntry {
        public ServiceStartMode? EnabledStartMode;
    }
}
