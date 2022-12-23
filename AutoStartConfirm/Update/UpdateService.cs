using AutoStartConfirm.Notifications;
using AutoStartConfirm.Properties;
using Octokit;
using Semver;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoStartConfirm.Update
{
    public class UpdateService : IUpdateService {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private SemVersion currentVersion;

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


        public IGitHubClient gitHubClient;
        private SettingsService SettingsService;
        private NotificationService NotificationService;

        public IGitHubClient GitHubClient
        {
            get
            {
                if (gitHubClient == null)
                {
                    gitHubClient = new GitHubClient(new ProductHeaderValue("AutoStartConfirm"));
                }
                return gitHubClient;
            }
            set
            {
                gitHubClient = value;
            }
        }


        public UpdateService(SettingsService settingsService, NotificationService notificationService) {
            SettingsService = settingsService;
            NotificationService = notificationService;
        }

        public async Task<Release> GetNewestRelease()
        {
            Logger.Trace("GetNewestRelease called");
            Release newestRelease;
            try
            {
                newestRelease = await GitHubClient.Repository.Release.GetLatest("ChristianGalla", "AutoStartConfirm");
                Logger.Trace("Got newest release");
                return newestRelease;
            }
            catch (Exception e)
            {
                var err = new Exception("Failed to get newest release from GitHub", e);
                Logger.Error(err);
                throw err;
            }
        }

        public SemVersion GetSemverVersion(Release release)
        {
            Logger.Trace("GetSemverVersion called");
            SemVersion newestVersion;
            try
            {
                newestVersion = SemVersion.Parse(release.TagName, SemVersionStyles.AllowLowerV | SemVersionStyles.OptionalPatch);
                Logger.Trace("GetSemverVersion finished");
                return newestVersion;
            }
            catch (Exception e)
            {
                var err = new Exception("Failed to parse release tag version", e);
                Logger.Error(err);
                throw err;
            }
        }

        public SemVersion GetCurrentVersion()
        {
            try
            {
                Logger.Trace("GetCurrentVersion called");
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var semverVersionString = $"{version.Major}.{version.Minor}.{version.Build}";
                var semverVersion = SemVersion.Parse(semverVersionString, SemVersionStyles.Strict);
                Logger.Trace("GetCurrentVersion finished");
                return semverVersion;
            }
            catch (Exception e)
            {
                var err = new Exception("Failed to get current version", e);
                Logger.Error(err);
                throw err;
            }
        }

        public async Task CheckUpdateAndShowNotification()
        {
            try
            {
                Logger.Trace("CheckUpdateAndShowNotification called");
                var newestRelease = await GetNewestRelease();
                var newestSemverVersion = GetSemverVersion(newestRelease);

                if (SettingsService.LastNotifiedNewVersion != null && SettingsService.LastNotifiedNewVersion.Length > 0)
                {
                    var lastNotifiedVersion = SemVersion.Parse(SettingsService.LastNotifiedNewVersion, SemVersionStyles.Strict);
                    if (newestSemverVersion.ComparePrecedenceTo(lastNotifiedVersion) <= 0)
                    {
                        Logger.Info("Already notified about newest version {version}", newestSemverVersion);
                        return;
                    }
                }
                if (newestSemverVersion.ComparePrecedenceTo(CurrentVersion) <= 0)
                {
                    Logger.Info("Current program is up to date");
                    return;
                }
                if (newestSemverVersion.Major > CurrentVersion.Major)
                {
                    Logger.Info("There is a new major version {version} available", newestSemverVersion);
                }
                if (newestSemverVersion.Minor > CurrentVersion.Minor)
                {
                    Logger.Info("There is a new minor version {version} available", newestSemverVersion);
                }
                if (newestSemverVersion.Patch > CurrentVersion.Patch)
                {
                    Logger.Info("There is a new patch version {version} available", newestSemverVersion);
                }

                string msiUrl = null;
                foreach (var asset in newestRelease.Assets)
                {
                    if (asset.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                    {
                        msiUrl = asset.BrowserDownloadUrl;
                        break;
                    }
                }
                SettingsService.LastNotifiedNewVersion = newestSemverVersion.ToString();
                NotificationService.ShowNewVersionNotification(newestSemverVersion.ToString(), CurrentVersion.ToString(), msiUrl);
                Logger.Trace("CheckUpdateAndShowNotification finished");
            }
            catch (Exception e)
            {
                var err = new Exception("Failed to check for updates", e);
                Logger.Error(err);
                throw err;
            }
        }

    }
}
