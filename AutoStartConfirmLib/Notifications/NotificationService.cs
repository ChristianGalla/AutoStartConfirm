using AutoStartConfirm.GUI;
using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace AutoStartConfirm.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> Logger;

        private readonly string assetDirectoryPath = $"{AppContext.BaseDirectory}Assets/";

        private readonly ResourceLoader ResourceLoader = new("AutoStartConfirmLib/Resources");

        public NotificationService(ILogger<NotificationService> logger) {
            Logger = logger;
        }

        public void ShowNewAutoStartEntryNotification(AutoStartEntry autostart)
        {
            try
            {
                Logger.LogTrace("ShowNewAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewAdd")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}AddIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText(ResourceLoader.GetString("Notification/Title/Add"), AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText(string.Format(ResourceLoader.GetString("Notification/Text/Category"), autostart.Category))
                    .AddButton(ResourceLoader.GetString("Notification/Button/Ok"), ToastActivationType.Background, new ToastArguments().Add("action", "confirmAdd").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton(ResourceLoader.GetString("Notification/Button/Disable"), ToastActivationType.Background, new ToastArguments().Add("action", "disable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton(ResourceLoader.GetString("Notification/Button/Revert"), ToastActivationType.Background, new ToastArguments().Add("action", "revertAdd").Add("id", autostart.Id.ToString()).ToString())
                    .Show();

                Logger.LogTrace("ShowNewAutoStartEntryNotification finished");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to show new auto start notification");
            }
        }

        public void ShowEnabledAutoStartEntryNotification(AutoStartEntry autostart)
        {
            try {
                Logger.LogTrace("ShowEnabledAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewAdd")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}AddIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText(ResourceLoader.GetString("Notification/Title/Enable"), AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText(string.Format(ResourceLoader.GetString("Notification/Text/Category"), autostart.Category))
                    .AddButton(ResourceLoader.GetString("Notification/Button/Ok"), ToastActivationType.Background, new ToastArguments().Add("action", "confirmEnable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton(ResourceLoader.GetString("Notification/Button/Disable"), ToastActivationType.Background, new ToastArguments().Add("action", "disable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton(ResourceLoader.GetString("Notification/Button/Remove"), ToastActivationType.Background, new ToastArguments().Add("action", "revertAdd").Add("id", autostart.Id.ToString()).ToString())
                    .Show();

                Logger.LogTrace("ShowEnabledAutoStartEntryNotification finished");
            } catch (Exception e) {
                Logger.LogError(e, "Failed to show enabled auto start notification");
            }
        }

        public void ShowRemovedAutoStartEntryNotification(AutoStartEntry autostart)
        {
            try {
                Logger.LogTrace("ShowRemovedAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewRemove")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}RemoveIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText(ResourceLoader.GetString("Notification/Title/Remove"), AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText(string.Format(ResourceLoader.GetString("Notification/Text/Category"), autostart.Category))
                    .AddButton(ResourceLoader.GetString("Notification/Button/Ok"), ToastActivationType.Background, new ToastArguments().Add("action", "confirmRemove").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton(ResourceLoader.GetString("Notification/Button/Revert"), ToastActivationType.Background, new ToastArguments().Add("action", "revertRemove").Add("id", autostart.Id.ToString()).ToString())
                    .Show();

                Logger.LogTrace("ShowRemovedAutoStartEntryNotification finished");
            } catch (Exception e) {
                Logger.LogError(e, "Failed to show removed auto start notification");
            }
        }

        public void ShowDisabledAutoStartEntryNotification(AutoStartEntry autostart)
        {
            try {
                Logger.LogTrace("ShowDisabledAutoStartEntryNotification called for {@autostart}", autostart);
                new ToastContentBuilder()
                    .AddArgument("action", "viewAdd")
                    .AddArgument("id", autostart.Id.ToString())
                    .AddAppLogoOverride(new Uri($"{assetDirectoryPath}RemoveIcon.png"), ToastGenericAppLogoCrop.None)
                    .AddText(ResourceLoader.GetString("Notification/Title/Disable"), AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText(string.Format(ResourceLoader.GetString("Notification/Text/Category"), autostart.Category))
                    .AddButton(ResourceLoader.GetString("Notification/Button/Ok"), ToastActivationType.Background, new ToastArguments().Add("action", "confirmDisable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton(ResourceLoader.GetString("Notification/Button/Enable"), ToastActivationType.Background, new ToastArguments().Add("action", "enable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton(ResourceLoader.GetString("Notification/Button/Remove"), ToastActivationType.Background, new ToastArguments().Add("action", "revertRemove").Add("id", autostart.Id.ToString()).ToString())
                    .Show();
                Logger.LogTrace("ShowDisabledAutoStartEntryNotification finished");
            } catch (Exception e) {
                Logger.LogError(e, "Failed to show disabled auto start notification");
            }
        }

        public void ShowNewVersionNotification(string newVersion, string currentVersion, string? msiUrl = null)
        {
            try
            {
                Logger.LogTrace("ShowNewVersionNotification called for {@newVersion}", newVersion);
                var toast = new ToastContentBuilder()
                    .AddArgument("action", "viewUpdate")
                    .AddText(ResourceLoader.GetString("Notification/Title/NewVersion"), AdaptiveTextStyle.Title)
                    .AddText(string.Format(ResourceLoader.GetString("Notification/Text/NewVersion"), newVersion))
                    .AddText(string.Format(ResourceLoader.GetString("Notification/Text/CurrentVersion"), currentVersion))
                    .AddButton(ResourceLoader.GetString("Notification/Button/Show"), ToastActivationType.Background, new ToastArguments().Add("action", "viewUpdate").ToString());
                if (msiUrl != null && msiUrl.Length > 0)
                {
                    toast = toast.AddButton("Install", ToastActivationType.Background, new ToastArguments().Add("action", "installUpdate").Add("msiUrl", msiUrl).ToString());
                }
                toast.Show();
                Logger.LogTrace("ShowNewVersionNotification finished");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to show disabled auto start notification");
            }
        }
    }
}
