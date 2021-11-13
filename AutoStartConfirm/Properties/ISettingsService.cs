using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;

namespace AutoStartConfirm.Properties {
    public interface ISettingsService: IDisposable {
        StringCollection DisabledConnectors { get; }

        event SettingChangingEventHandler SettingChanging;
        event PropertyChangedEventHandler PropertyChanged;
        event SettingsSavingEventHandler SettingsSaving;
        event SettingsLoadedEventHandler SettingsLoaded;

        void EnsureConfiguration();

        void Save();
    }
}