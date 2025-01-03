using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;

namespace AutoStartConfirm.Properties
{
    public class SettingsService : ISettingsService, IDisposable
    {

        private readonly ILogger<SettingsService> Logger;

        private bool disposedValue;

        /// <summary>
        /// Ensures a valid configuration exists and upgrades configuration from a previous version if needed
        /// </summary>
        private void EnsureConfiguration()
        {
            Logger.LogDebug("Ensuring configuration");
            if (Settings.Default.UpgradeRequired)
            {
                Logger.LogInformation("Upgrading configuration");
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
                Logger.LogInformation("Configuration upgraded");
            }
            Logger.LogDebug("Ensured configuration");
        }

        public StringCollection DisabledConnectors
        {
            get
            {
                if (Settings.Default.DisabledConnectors == null)
                {
                    Settings.Default.DisabledConnectors = new StringCollection();
                }
                return Settings.Default.DisabledConnectors;
            }
            set
            {
                Settings.Default.DisabledConnectors = value;
                Save();
            }
        }

        public bool CheckForUpdatesOnStart
        {
            get
            {
                return Settings.Default.CheckForUpdatesOnStart;
            }
            set
            {
                Settings.Default.CheckForUpdatesOnStart = value;
                Save();
            }
        }

        public string LastNotifiedNewVersion
        {
            get
            {
                return Settings.Default.LastNotifiedNewVersion;
            }
            set
            {
                Settings.Default.LastNotifiedNewVersion = value;
                Save();
            }
        }


        /// <summary>
        /// Occurs before the value of an application settings property is changed.
        /// </summary>
        public event SettingChangingEventHandler? SettingChanging;

        /// <summary>
        /// Occurs after the value of an application settings property is changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Occurs before values are saved to the data store.
        /// </summary>
        public event SettingsSavingEventHandler? SettingsSaving;

        /// <summary>
        /// Occurs after the application settings are retrieved from storage.
        /// </summary>
        public event SettingsLoadedEventHandler? SettingsLoaded;

        /// <summary>
        /// Stores the current values of the application settings properties.
        /// </summary>
        public void Save() => Settings.Default.Save();

        public SettingsService(ILogger<SettingsService> logger)
        {
            Logger = logger;
            EnsureConfiguration();
            Settings.Default.SettingChanging += SettingChangingHandler;
            Settings.Default.PropertyChanged += PropertyChangedHandler;
            Settings.Default.SettingsSaving += SettingsSavingHandler;
            Settings.Default.SettingsLoaded += SettingsLoadedHandler;
        }

        private void SettingChangingHandler(object sender, SettingChangingEventArgs e)
        {
            SettingChanging?.Invoke(sender, e);
        }

        private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        private void SettingsSavingHandler(object sender, CancelEventArgs e)
        {
            Logger.LogInformation("Settings saving");
            SettingsSaving?.Invoke(sender, e);
        }

        private void SettingsLoadedHandler(object sender, SettingsLoadedEventArgs e)
        {
            Logger.LogInformation("Settings loaded");
            SettingsLoaded?.Invoke(sender, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Settings.Default.SettingChanging -= SettingChangingHandler;
                    Settings.Default.PropertyChanged -= PropertyChangedHandler;
                    Settings.Default.SettingsSaving -= SettingsSavingHandler;
                    Settings.Default.SettingsLoaded -= SettingsLoadedHandler;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
