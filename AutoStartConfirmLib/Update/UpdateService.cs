using AutoStartConfirm.Notifications;
using AutoStartConfirm.Properties;
using Microsoft.Extensions.Logging;
using NLog;
using Octokit;
using Semver;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AutoStartConfirm.Update
{
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> Logger;

        private SemVersion? currentVersion = null;

        public SemVersion CurrentVersion
        {
            get
            {
                if (currentVersion == null)
                {
                    currentVersion = GetCurrentVersion();
                }
                return currentVersion;
            }
            set
            {
                currentVersion = value;
            }
        }

        public bool IsStandalone { get; set; }


        private readonly ISettingsService SettingsService;
        private readonly INotificationService NotificationService;

        private readonly IGitHubClient GitHubClient;

        public UpdateService(ILogger<UpdateService> logger, ISettingsService settingsService, INotificationService notificationService, IGitHubClient gitHubClient) {
            Logger = logger;
            SettingsService = settingsService;
            NotificationService = notificationService;
            GitHubClient = gitHubClient;

#if FRAMEWORK_DEPENDENT
            IsStandalone = false;
#endif
#if STANDALONE
            IsStandalone = true;
#endif
        }

        public async Task<Release> GetNewestRelease()
        {
            Logger.LogTrace("GetNewestRelease called");
            Release newestRelease;
            try
            {
                newestRelease = await GitHubClient.Repository.Release.GetLatest("ChristianGalla", "AutoStartConfirm");
                Logger.LogTrace("Got newest release");
                return newestRelease;
            }
            catch (Exception e)
            {
                const string message = "Failed to get newest release from GitHub";
                Logger.LogError(e, message);
                throw new Exception(message, e);
            }
        }

        public SemVersion GetSemverVersion(Release release)
        {
            Logger.LogTrace("GetSemverVersion called");
            SemVersion newestVersion;
            try
            {
                newestVersion = SemVersion.Parse(release.TagName, SemVersionStyles.AllowLowerV | SemVersionStyles.OptionalPatch);
                Logger.LogTrace("GetSemverVersion finished");
                return newestVersion;
            }
            catch (Exception e)
            {
                const string message = "Failed to parse release tag version";
                Logger.LogError(e, message);
                throw new Exception(message, e);
            }
        }

        public SemVersion GetCurrentVersion()
        {
            try
            {
                Logger.LogTrace("GetCurrentVersion called");
                var version = Assembly.GetEntryAssembly()!.GetName().Version!;
                var semverVersionString = $"{version.Major}.{version.Minor}.{version.Build}";
                var semverVersion = SemVersion.Parse(semverVersionString, SemVersionStyles.Strict);
                Logger.LogTrace("GetCurrentVersion finished");
                return semverVersion;
            }
            catch (Exception e)
            {
                const string message = "Failed to get current version";
                Logger.LogError(e, message);
                throw new Exception(message, e);
            }
        }

        public async Task CheckUpdateAndShowNotification()
        {
            try
            {
                Logger.LogTrace("CheckUpdateAndShowNotification called");
                if (IsStandalone)
                {
                    Logger.LogInformation("Checking for standalone msi");
                }
                else
                {
                    Logger.LogInformation("Checking for framework dependent msi");
                }

                var newestRelease = await GetNewestRelease();
                var newestSemverVersion = GetSemverVersion(newestRelease);

                if (SettingsService.LastNotifiedNewVersion != null && SettingsService.LastNotifiedNewVersion.Length > 0)
                {
                    var lastNotifiedVersion = SemVersion.Parse(SettingsService.LastNotifiedNewVersion, SemVersionStyles.Strict);
                    if (newestSemverVersion.ComparePrecedenceTo(lastNotifiedVersion) <= 0)
                    {
                        Logger.LogInformation("Already notified about newest version {version}", newestSemverVersion);
                        return;
                    }
                }
                if (newestSemverVersion.ComparePrecedenceTo(CurrentVersion) <= 0)
                {
                    Logger.LogInformation("Current program is up to date");
                    return;
                }
                if (newestSemverVersion.Major > CurrentVersion.Major)
                {
                    Logger.LogInformation("There is a new major version {version} available", newestSemverVersion);
                }
                if (newestSemverVersion.Minor > CurrentVersion.Minor)
                {
                    Logger.LogInformation("There is a new minor version {version} available", newestSemverVersion);
                }
                if (newestSemverVersion.Patch > CurrentVersion.Patch)
                {
                    Logger.LogInformation("There is a new patch version {version} available", newestSemverVersion);
                }

                string? msiUrl = null;
                foreach (var asset in newestRelease.Assets)
                {
                    if (!asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                        asset.Name.Contains("Debug", StringComparison.OrdinalIgnoreCase) ||
                        IsStandalone && !asset.Name.Contains("Standalone", StringComparison.OrdinalIgnoreCase) ||
                        !IsStandalone && !asset.Name.Contains("FrameworkDependent", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    msiUrl = asset.BrowserDownloadUrl;
                    break;
                }
                if (msiUrl == null)
                {
                    Logger.LogWarning("No fitting msi found");
                    foreach (var asset in newestRelease.Assets)
                    {
                        if (!asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                            asset.Name.Contains("Debug", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        msiUrl = asset.BrowserDownloadUrl;
                        break;
                    }
                }
                if (msiUrl == null)
                {
                    Logger.LogError("No msi found");
                    return;
                }
                SettingsService.LastNotifiedNewVersion = newestSemverVersion.ToString();
                NotificationService.ShowNewVersionNotification(newestSemverVersion.ToString(), CurrentVersion.ToString(), msiUrl);
                Logger.LogTrace("CheckUpdateAndShowNotification finished");
            }
            catch (Exception e)
            {
                const string message = "Failed to check for updates";
                Logger.LogError(e, message);
                throw new Exception(message, e); ;
            }
        }

    }
}
