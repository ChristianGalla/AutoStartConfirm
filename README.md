# Auto Start Confirm

More and more programs want to start automatically when Windows starts or a user logs on.

Many startup programs can slow down the boot process.
In addition, malicious software, such as keyloggers, can survive reboots.

Therefore, this program monitors whether a program wants to start automatically and asks the user for permission.

## Warning

Some useful programs require services to start automatically.
When their auto start is blocked, they may not function properly.
Also, a blocked update service may lead to insecure environments because of not patched security bugs in old program versions.

Therefore, usually auto starts should not be blocked.

## State of development

The development has just begun.
The program can not be used yet.

Currently, the following startup locations are being monitored:

- [x] Boot execute
- [ ] Appinit DLLs
- [ ] Explorer Addons
- [ ] Image hijacks
- [ ] Internet Explorer Addons
- [ ] Known DLLs
- [ ] Logon
- [ ] Winsock
- [ ] Codecs
- [ ] Office Add-Ins
- [ ] Print monitor DLLs
- [ ] LSA security providers
- [ ] Services and drivers
- [ ] Scheduled Tasks
- [ ] Winlogon
- [ ] WMI

## Links

This program is similar to [Sysinternals Autoruns](https://docs.microsoft.com/en-us/sysinternals/downloads/autoruns).
Sysinternals Autoruns is a great tool for analyzing and disabling or enabling existing autostart programs.
However, it lacks a function to notify a user about a new startup program and to ask for his permission.

Sysinternals Autoruns is not an Open Source program, but there is a [Autoruns PowerShell Module](https://github.com/p0w3rsh3ll/AutoRuns)
that can be used, for example, to determine where a program can be registered to start automatically with Windows.

## How to build and debug

This program was created using Visual Studio 2019.

There are some NuGet dependencies that must be installed before it can be compiled.

The installer is build using [WiX Toolset build tools](https://wixtoolset.org/releases/) and the WiX Toolset Visual Studio 2019 Extension.

For Toast Notifications to appear it is required to have a Start menu shortcut which can be created using the installer.
