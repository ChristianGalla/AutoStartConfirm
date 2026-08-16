# AutoStartConfirm - Copilot Instructions

Windows tray app (WinUI 3 / Windows App SDK) that monitors Windows auto-start
locations (registry Run keys, services, scheduled tasks, startup folders,
group policy scripts, etc.) and asks the user to confirm newly added or
removed auto-starts.

## Solution layout

- `AutoStartConfirm/` – only the application entry point. `Program.cs`
  (`Main`) registers services via `Program.ConfigureServices` and resolves
  them via `Ioc.Default` (CommunityToolkit.Mvvm) / constructor injection.
  The actual WinUI 3 GUI (tray icon, windows, pages, notifications) lives
  in `AutoStartConfirmLib/GUI/`, not in this project.
- `AutoStartConfirmLib/` – class library with all business logic **and**
  the GUI:
  - `GUI/` – WinUI 3 UI (`MainWindow`, `MainPage`, `SettingsPage`,
    `AboutPage`, `NotifyIcon` tray icon, `MessageService`/`IMessageService`,
    `ISubPage`). Treat this as the app's UI layer even though it is
    physically part of `AutoStartConfirmLib` rather than `AutoStartConfirm`.
  - `Connectors/` – one connector class + interface pair per auto-start
    location (e.g. `CurrentUserRun64Connector`/`ICurrentUserRun64Connector`,
    `ServiceConnector`, `ScheduledTaskConnector`, `RegistryConnector`
    subfolders `Registry/`, `ScheduledTask/`, `Services/`, `Folder/`). All
    implement `IAutoStartConnector` (add/remove/enable/disable, watcher
    start/stop, `Add`/`Remove`/`Enable`/`Disable` events). New auto-start
    locations should follow this same interface + one-class-per-location
    pattern, and be registered in
    `Connectors/AutoStartConnectorService.cs` (`ConfigureServices`), which
    is itself called from `Program.ConfigureServices`.
  - `Business/AutoStartBusiness.cs` (`IAutoStartBusiness`) – aggregates all
    connectors, tracks history/confirmation state, is the main entry point
    used by the UI and by command-line revert/enable/disable actions.
  - `Models/` – POCOs such as `AutoStartEntry` and subtype entries
    (`RegistryAutoStartEntry`, `ServiceAutoStartEntry`,
    `ScheduledTaskAutoStartEntry`, `FolderAutoStartEntry`), `Category`,
    `Change`, `ConfirmStatus`.
  - `Helpers/` – cross-cutting services (`IDispatchService`, `IUacService`,
    JSON helpers).
  - `Notifications/`, `Update/`, `Exceptions/`, `Strings/` – toast
    notifications, self-update via Octokit/GitHub releases, custom
    exceptions, localized strings.
- `AutoStartConfirmSetup/` – WiX installer project (produces
  `AutoStartConfirmSetup_Standalone.msi` and
  `AutoStartConfirmSetup_FrameworkDependent.msi`).
- `AutoStartConfirmTest/` – **current** MSTest test project (net10,
  referenced in `AutoStartConfirm.sln`). Tests live under `Business/` and
  reuse `TestsBase.cs` for shared `FakeItEasy` fakes.
- `AutoStartConfirmTests/` – older duplicate test project (net8), not part
  of the `.sln` but still built/run by `.github/workflows/ci.yml`. Keep
  these two projects' test files in sync until the CI workflow and solution
  are unified onto a single test project; check both when adding/changing
  tests for `AutoStartBusiness`.
- `Build/` – `Daily.targets`/`Daily_Debug.bat`/`Daily_Release.bat` drive
  MSBuild + WiX packaging for local/CI builds.

## Key conventions

- Every injectable service/connector has an `I<Name>` interface and is
  registered as a singleton in `Program.ConfigureServices` (app) or
  `AutoStartConnectorService.ConfigureServices` (connectors). Follow this
  pattern for new services instead of `new`-ing dependencies directly.
- Build configurations are `Debug|Release` × `Framework Dependent|Standalone`
  (not the default `Debug|Release`). `FRAMEWORK_DEPENDENT`/`STANDALONE` and
  `DEBUG`/`RELEASE` preprocessor constants are set per configuration in
  `AutoStartConfirmLib.csproj`/`AutoStartConfirm.csproj` — code that differs
  between standalone and framework-dependent builds uses `#if
  FRAMEWORK_DEPENDENT` / `#if STANDALONE`.
- Auto-start add/remove/enable/disable actions can be reverted from a toast
  notification, which relaunches the app with command-line flags
  (`AutoStartBusiness.RevertAddParameterName`, `RevertRemoveParameterName`,
  `EnableParameterName`, `DisableParameterName` + a path to an XML-serialized
  `AutoStartEntry`); see `App.HandleCommandLineParameters`.
- Logging uses NLog (`nlog.config`) via `Microsoft.Extensions.Logging`
  (`ILogger<T>` injected), not `Console.WriteLine`/`Debug.WriteLine`.
- All persisted `AutoStartEntry` objects are XML-serialized
  (`XmlSerializer`), not binary — a past breaking change removed the binary
  deserializer for security reasons; don't reintroduce binary
  serialization for settings/history.

## Build & test

Requires Windows + Visual Studio (WinUI 3 / Windows App SDK workload) or the
.NET 8/10 SDKs with `msbuild`. There is no `dotnet build`-only path for the
WinUI app because of MSIX/WiX packaging; CI uses `msbuild`/`vstest.console.exe`
(see `.github/workflows/ci.yml`):

```powershell
# Restore
nuget restore AutoStartConfirm.sln

# Build app + installers (release)
msbuild build/Daily.targets /property:Configuration=Release
# or for local debug/release installer builds:
Build\Daily_Debug.bat
Build\Daily_Release.bat

# Build & run all tests (matches CI, uses AutoStartConfirmTests project)
dotnet restore AutoStartConfirmTests\AutoStartConfirmTests.csproj
msbuild AutoStartConfirmTests\AutoStartConfirmTests.csproj -p:Configuration=Release -p:Platform=x64 -p:PublishReadyToRun=false -p:OutputPath="bin/x64/Release/win-x64/"
vstest.console.exe /Platform:x64 "AutoStartConfirmTests\bin\x64\Release\win-x64\AutoStartConfirmTests.dll"

# Run a single test class/method (vstest.console.exe filter)
vstest.console.exe /Platform:x64 /Tests:AutoStartBusinessTests.SomeTestMethod "AutoStartConfirmTests\bin\x64\Release\win-x64\AutoStartConfirmTests.dll"
```

For the solution's current test project (`AutoStartConfirmTest`, net10),
build/run the same way but point at
`AutoStartConfirmTest\AutoStartConfirmTest.csproj` and its output DLL.

To regenerate third-party license files (required before building
`AutoStartConfirmLib` if `Licenses/` is missing):

```powershell
dotnet tool install --global dotnet-project-licenses
dotnet-project-licenses -i AutoStartConfirmLib -o AutoStartConfirmLib\Licenses -t --timeout 60 -e -c -f AutoStartConfirmLib\Licenses -u
```
