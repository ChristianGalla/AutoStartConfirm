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

        private INotificationService notificationService;

        public INotificationService NotificationService
        {
            get
            {
                if (notificationService == null)
                {
                    notificationService = new NotificationService();
                }
                return notificationService;
            }
            set
            {
                notificationService = value;
            }
        }

        private Task<SemVersion> newestVersion;

        public Task<SemVersion> NewestVersion
        {
            get
            {
                if (newestVersion == null)
                {
                    newestVersion = GetNewestVersion();
                }
                return newestVersion;
            }
            set
            {
                newestVersion = value;
            }
        }


        private ISettingsService settingsService;

        public ISettingsService SettingsService
        {
            get
            {
                if (settingsService == null)
                {
                    settingsService = new SettingsService();
                }
                return settingsService;
            }
            set
            {
                settingsService = value;
            }
        }

        public UpdateService() {
        }

        public async Task<SemVersion> GetNewestVersion()
        {
            try
            {
                Logger.Trace("GetNewestVersion called");
                Release newestRelease;
                try
                {
                    var client = new GitHubClient(new ProductHeaderValue("AutoStartConfirm"));
                    newestRelease = await client.Repository.Release.GetLatest("ChristianGalla", "AutoStartConfirm");
                    Logger.Trace("Got newest release");
                }
                catch (Exception e)
                {
                    var err = new Exception("Failed to get newest release from GitHub", e);
                    throw err;
                }
                SemVersion newestVersion;
                try
                {
                    newestVersion = SemVersion.Parse(newestRelease.TagName, SemVersionStyles.AllowLowerV | SemVersionStyles.OptionalPatch);
                    Logger.Trace("GetNewestVersion finished");
                    return newestVersion;
                }
                catch (Exception e)
                {
                    var err = new Exception("Failed to parse newest release tag version", e);
                    throw err;
                }
            }
            catch (Exception e)
            {
                var err = new Exception("Failed to get newest version", e);
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
                var newestVersion = await NewestVersion;
                if (SettingsService.LastNotifiedNewVersion != null && SettingsService.LastNotifiedNewVersion.Length > 0)
                {
                    var lastNotifiedVersion = SemVersion.Parse(SettingsService.LastNotifiedNewVersion, SemVersionStyles.Strict);
                    if (newestVersion.ComparePrecedenceTo(lastNotifiedVersion) <= 0)
                    {
                        Logger.Info("Already notified about newest version {version}", newestVersion);
                        return;
                    }
                }
                if (newestVersion.ComparePrecedenceTo(CurrentVersion) <= 0)
                {
                    Logger.Info("Current program is up to date");
                    return;
                }
                if (newestVersion.Major > CurrentVersion.Major)
                {
                    Logger.Info("There is a new major version {version} available", newestVersion);
                }
                if (newestVersion.Minor > CurrentVersion.Minor)
                {
                    Logger.Info("There is a new minor version {version} available", newestVersion);
                }
                if (newestVersion.Patch > CurrentVersion.Patch)
                {
                    Logger.Info("There is a new patch version {version} available", newestVersion);
                }
                SettingsService.LastNotifiedNewVersion = newestVersion.ToString();
                NotificationService.ShowNewVersionNotification(newestVersion.ToString(), CurrentVersion.ToString());
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
