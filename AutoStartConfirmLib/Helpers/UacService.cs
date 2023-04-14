using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System;

namespace AutoStartConfirm.Helpers
{
    // source: https://stackoverflow.com/questions/1220213/detect-if-running-as-administrator-with-or-without-elevated-privileges
    public class UacService : IUacService
    {
        private const string uacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string uacRegistryValue = "EnableLUA";

        private static readonly uint STANDARD_RIGHTS_READ = 0x00020000;
        private static readonly uint TOKEN_QUERY = 0x0008;
        private static readonly uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        private enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        private bool? isUacEnabled;

        public bool IsUacEnabled
        {
            get
            {
                if (isUacEnabled == null)
                {
                    using RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(uacRegistryKey, false)!;
                    isUacEnabled = uacKey.GetValue(uacRegistryValue)?.Equals(1) ?? false;
                }
                return isUacEnabled.Value;
            }
        }

        private bool? isProcessElevated;

        public bool IsProcessElevated
        {
            get
            {
                if (isProcessElevated == null)
                {
                    if (IsUacEnabled)
                    {
                        IntPtr tokenHandle = IntPtr.Zero;
                        if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_READ, out tokenHandle))
                        {
                            throw new ApplicationException("Could not get process token.  Win32 Error Code: " +
                                                           Marshal.GetLastWin32Error());
                        }

                        try
                        {
                            TOKEN_ELEVATION_TYPE elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;

                            int elevationResultSize = sizeof(TOKEN_ELEVATION_TYPE);
                            uint returnedSize = 0;

                            IntPtr elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);
                            try
                            {
                                bool success = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType,
                                                                   elevationTypePtr, (uint)elevationResultSize,
                                                                   out returnedSize);
                                if (success)
                                {
                                    elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
                                    isProcessElevated = elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                                }
                                else
                                {
                                    throw new ApplicationException("Unable to determine the current elevation.");
                                }
                            }
                            finally
                            {
                                if (elevationTypePtr != IntPtr.Zero)
                                    Marshal.FreeHGlobal(elevationTypePtr);
                            }
                        }
                        finally
                        {
                            if (tokenHandle != IntPtr.Zero)
                                CloseHandle(tokenHandle);
                        }
                    }
                    else
                    {
                        WindowsIdentity identity = WindowsIdentity.GetCurrent();
                        WindowsPrincipal principal = new WindowsPrincipal(identity);
                        isProcessElevated = principal.IsInRole(WindowsBuiltInRole.Administrator)
                                   || principal.IsInRole(0x200); //Domain Administrator
                    }
                }

                return isProcessElevated.Value;
            }
        }
    }
}
