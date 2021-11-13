﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Properties {
    public class SettingsService : ISettingsService, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool disposedValue;

        /// <summary>
        /// Ensures a valid configuration exists and upgrades configuration from a previous version if needed
        /// </summary>
        public void EnsureConfiguration() {
            Logger.Debug("Ensuring configuration");
            if (Settings.Default.UpgradeRequired) {
                Logger.Info("Upgrading configuration");
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
                Logger.Info("Configuration upgraded");
            }
            if (Settings.Default.DisabledConnectors == null) {
                Settings.Default.DisabledConnectors = new StringCollection();
            }
            Logger.Debug("Ensured configuration");
        }

        public StringCollection DisabledConnectors => Settings.Default.DisabledConnectors;


        /// <summary>
        /// Occurs before the value of an application settings property is changed.
        /// </summary>
        public event SettingChangingEventHandler SettingChanging;

        /// <summary>
        /// Occurs after the value of an application settings property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs before values are saved to the data store.
        /// </summary>
        public event SettingsSavingEventHandler SettingsSaving;

        /// <summary>
        /// Occurs after the application settings are retrieved from storage.
        /// </summary>
        public event SettingsLoadedEventHandler SettingsLoaded;

        /// <summary>
        /// Stores the current values of the application settings properties.
        /// </summary>
        public void Save() => Settings.Default.Save();

        public SettingsService() {
            Settings.Default.SettingChanging += SettingChangingHandler;
            Settings.Default.PropertyChanged += PropertyChangedHandler;
            Settings.Default.SettingsSaving += SettingsSavingHandler;
            Settings.Default.SettingsLoaded += SettingsLoadedHandler;
        }

        private void SettingChangingHandler(object sender, SettingChangingEventArgs e) {
            SettingChanging?.Invoke(sender, e);
        }

        private void PropertyChangedHandler(object sender, PropertyChangedEventArgs e) {
            PropertyChanged?.Invoke(sender, e);
        }

        private void SettingsSavingHandler(object sender, CancelEventArgs e) {
            SettingsSaving?.Invoke(sender, e);
        }

        private void SettingsLoadedHandler(object sender, SettingsLoadedEventArgs e) {
            SettingsLoaded?.Invoke(sender, e);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Settings.Default.SettingChanging -= SettingChangingHandler;
                    Settings.Default.PropertyChanged -= PropertyChangedHandler;
                    Settings.Default.SettingsSaving -= SettingsSavingHandler;
                    Settings.Default.SettingsLoaded -= SettingsLoadedHandler;
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
