using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System;

namespace AutoStartConfirm.Helpers
{
    public class UacService : IUacService
    {
        public bool IsProcessElevated => System.Environment.IsPrivilegedProcess;
    }
}
