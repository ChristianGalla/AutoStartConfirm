# Auto Start Confirm

[![Build & Test](https://github.com/ChristianGalla/AutoStartConfirm/actions/workflows/main.yml/badge.svg)](https://github.com/ChristianGalla/AutoStartConfirm/actions/workflows/main.yml)

More and more programs want to start automatically when Windows starts, or a user logs on.

Many startup programs can slow down the boot process.
In addition, malicious software, such as keyloggers, can survive reboots.

Therefore, this program monitors whether a program wants to start automatically and asks the user for permission.

## Table of contents

* [User documentation](#user-documentation)
* [Usage warning](#usage-warning)
* [State of development](#state-of-development)
* [Current limitations](#current-limitations)
* [Similar programs](#similar-programs)
* [How to build and debug](#how-to-build-and-debug)

## User documentation

The program starts in the background and can be accessed using its icon in the notification area
(usually in the bottom right corner of the taskbar).

![Main Window](./Images/MainWindow.png "Main Window")

All currently known auto starts are listed in the "Current" tab.
A history of added or removed auto starts can be accessed using the "Add history" and "Remove history" tabs.

For each auto start there is a "Confirm" and "Remove" button to easily mark checked auto starts and remove unwanted ones.

When a new auto start is added or removed, a toast notification appears:

![Add Notification](./Images/AddNotification.png "Add Notification")

![Remove Notification](./Images/RemoveNotification.png "Remove Notification")

A click on the "Ok" button confirms the change and a click on the "Revert" button undoes it.

When the program is started, it compares the current auto starts to the known ones when it last run.
Therefore, changes will be detected, even if the program is not running all time.

## Usage warning

Some useful programs require services to start automatically.
When their auto start is blocked, they may not function properly.
Also, for example a blocked update service may lead to insecure environments because of not patched security bugs in old program versions.

Therefore, auto starts should only be blocked if there is no negative impact on the affected programs.

## State of development

The core functionality of Auto Start Confirm is implemented but needs some testing and polishing.

There are many locations Windows provides for programs to start automatically.
Currently only the following locations are monitored by Auto Start Confirm.

### Currently monitored

- [x] Boot execute
- [x] Appinit DLLs
- [x] Logon
- [x] Scheduled Tasks
- [x] Services and drivers

### Implementation currently not planned

- [ ] Winlogon
- [ ] Known DLLs
- [ ] Explorer Addons
- [ ] Image hijacks
- [ ] Winsock
- [ ] LSA security providers
- [ ] Print monitor DLLs
- [ ] Codecs
- [ ] Internet Explorer Addons
- [ ] Office Add-Ins
- [ ] WMI

For details see the parameters of the [Autoruns PowerShell Module](https://github.com/p0w3rsh3ll/AutoRuns).

## Current limitations

To be able to use as less privileges as needed, Auto Start Confirm currently runs in user mode and therefore can only monitor and react after changes occurred.
There are a few advantages of this implementation, for example even users that are not administrators are able to run the program.
Currently, it is not planned to change this implementation.

Currently, it is not planned to create a 32-bit version of the program because most Windows installations are already 64-bit and additional work is needed
for example, to access 64-bit registry keys from a 32-bit program.

## Similar programs

This program is similar to [Sysinternals Autoruns](https://docs.microsoft.com/en-us/sysinternals/downloads/autoruns).
Sysinternals Autoruns is a great tool for analyzing and disabling or enabling existing autostart programs.
However, it lacks the function to monitor auto start locations in background and ask users for permission when changes occurred.

Sysinternals Autoruns is not an Open Source program, but there is a [Autoruns PowerShell Module](https://github.com/p0w3rsh3ll/AutoRuns)
that was used as reference to determine where Auto Start Confirm should look for auto start locations.

## How to build and debug

This program was created using Visual Studio 2019.

There are some NuGet dependencies that must be installed before it can be compiled.

The installer is build using [WiX Toolset build tools](https://wixtoolset.org/releases/) and the WiX Toolset Visual Studio 2019 Extension.
