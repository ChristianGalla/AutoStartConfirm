using AutoStartConfirm.Models;

namespace AutoStartConfirm.Notifications {
    public interface INotificationService {
        void ShowDisabledAutoStartEntryNotification(AutoStartEntry autostart);
        void ShowEnabledAutoStartEntryNotification(AutoStartEntry autostart);
        void ShowNewAutoStartEntryNotification(AutoStartEntry addedAutostart);
        void ShowRemovedAutoStartEntryNotification(AutoStartEntry removedAutostart);
    }
}