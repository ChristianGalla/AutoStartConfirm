using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;

namespace AutoStartConfirm.Properties
{
    public interface ISettingsService : IDisposable
    {
        StringCollection DisabledConnectors { get; set; }

        bool CheckForUpdatesOnStart { get; set; }

        string LastNotifiedNewVersion { get; set; }

        event SettingChangingEventHandler SettingChanging;
        event PropertyChangedEventHandler PropertyChanged;
        event SettingsSavingEventHandler SettingsSaving;
        event SettingsLoadedEventHandler SettingsLoaded;

        void Save();
    }
}