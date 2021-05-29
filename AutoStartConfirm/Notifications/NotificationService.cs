using AutoStartConfirm.AutoStarts;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace AutoStartConfirm.Notifications {
    public class NotificationService : INotificationService {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string assetDirectoryPath = $"{AppContext.BaseDirectory}Assets/";

        public NotificationService() {
        }

        public void ShowNewAutoStartEntryNotification(AutoStartEntry autostart) {
            try {
                Logger.Trace("ShowNewAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewAdd")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}AddIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText("Autostart added", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmAdd").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Disable", ToastActivationType.Background, new ToastArguments().Add("action", "disable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Revert", ToastActivationType.Background, new ToastArguments().Add("action", "revertAdd").Add("id", autostart.Id.ToString()).ToString())
                    .Show();

                Logger.Trace("ShowNewAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show new auto start notification", e);
                Logger.Error(err);
            }
        }

        public void ShowEnabledAutoStartEntryNotification(AutoStartEntry autostart) {
            try {
                Logger.Trace("ShowEnabledAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewAdd")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}AddIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText("Autostart enabled", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmEnable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Disable", ToastActivationType.Background, new ToastArguments().Add("action", "disable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Remove", ToastActivationType.Background, new ToastArguments().Add("action", "revertAdd").Add("id", autostart.Id.ToString()).ToString())
                    .Show();

                Logger.Trace("ShowEnabledAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show enabled auto start notification", e);
                Logger.Error(err);
            }
        }

        public void ShowRemovedAutoStartEntryNotification(AutoStartEntry autostart) {
            try {
                Logger.Trace("ShowRemovedAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewRemove")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}RemoveIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText("Autostart removed", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmRemove").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Revert", ToastActivationType.Background, new ToastArguments().Add("action", "revertRemove").Add("id", autostart.Id.ToString()).ToString())
                    .Show();

                Logger.Trace("ShowRemovedAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show removed auto start notification", e);
                Logger.Error(err);
            }
        }

        public void ShowDisabledAutoStartEntryNotification(AutoStartEntry autostart) {
            try {
                Logger.Trace("ShowDisabledAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewAdd")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}RemoveIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText("Autostart disabled", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmDisable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Enable", ToastActivationType.Background, new ToastArguments().Add("action", "enable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Remove", ToastActivationType.Background, new ToastArguments().Add("action", "revertRemove").Add("id", autostart.Id.ToString()).ToString())
                    .Show();
                Logger.Trace("ShowDisabledAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show disabled auto start notification", e);
                Logger.Error(err);
            }
        }
    }
}
