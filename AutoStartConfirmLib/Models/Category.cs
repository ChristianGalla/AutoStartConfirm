﻿namespace AutoStartConfirm.Models
{
    public enum Category
    {
        BootExecute,
        AppInit32,
        AppInit64,
        AppCertDll,
        Winlogon,
        UserInitMprLogonScript,
        CurrentUserUserInitMprLogonScript,
        GroupPolicyExtensions,
        DomainGroupPolicyScriptStartup,
        DomainGroupPolicyScriptShutdown,
        DomainGroupPolicyScriptLogon,
        DomainGroupPolicyScriptLogoff,
        LocalGroupPolicyScriptStartup,
        LocalGroupPolicyScriptShutdown,
        LocalGroupPolicyScriptLogon,
        LocalGroupPolicyScriptLogoff,
        CurrentUserLocalGroupPolicyScriptStartup,
        CurrentUserLocalGroupPolicyScriptShutdown,
        CurrentUserLocalGroupPolicyScriptLogon,
        CurrentUserLocalGroupPolicyScriptLogoff,
        GroupPolicyShellOverwrite,
        CurrentUserGroupPolicyShellOverwrite,
        AlternateShell,
        AvailableShells,
        TerminalServerStartupPrograms,
        TerminalServerRun,
        TerminalServerRunOnce,
        TerminalServerRunOnceEx,
        CurrentUserTerminalServerRun,
        CurrentUserTerminalServerRunOnce,
        CurrentUserTerminalServerRunOnceEx,
        TerminalServerInitialProgram,
        Run32,
        RunOnce32,
        RunOnceEx32,
        CurrentUserRun32,
        CurrentUserRunOnce32,
        CurrentUserRunOnceEx32,
        Run64,
        RunOnce64,
        RunOnceEx64,
        CurrentUserRun64,
        CurrentUserRunOnce64,
        CurrentUserRunOnceEx64,
        GroupPolicyRun,
        CurrentUserGroupPolicyRun,
        ActiveSetup32,
        ActiveSetup64,
        IconServiceLib,
        WindowsCEServicesAutoStartOnConnect32,
        WindowsCEServicesAutoStartOnDisconnect32,
        WindowsCEServicesAutoStartOnConnect64,
        WindowsCEServicesAutoStartOnDisconnect64,
        CurrentUserLoad,
        StartMenuAutoStartFolder,
        CurrentUserStartMenuAutoStartFolder,
        ScheduledTask,
        DeviceService,
        Service,
    }
}
