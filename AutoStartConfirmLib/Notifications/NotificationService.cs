using AutoStartConfirm.GUI;
using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading.Tasks;

namespace AutoStartConfirm.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> Logger;

        private readonly string assetDirectoryPath = $"{AppContext.BaseDirectory}Assets/";

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
                    .AddText("Auto start added", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmAdd").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Disable", ToastActivationType.Background, new ToastArguments().Add("action", "disable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Revert", ToastActivationType.Background, new ToastArguments().Add("action", "revertAdd").Add("id", autostart.Id.ToString()).ToString())
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
                    .AddText("Auto start enabled", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmEnable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Disable", ToastActivationType.Background, new ToastArguments().Add("action", "disable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Remove", ToastActivationType.Background, new ToastArguments().Add("action", "revertAdd").Add("id", autostart.Id.ToString()).ToString())
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
                    .AddText("Auto start removed", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmRemove").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Revert", ToastActivationType.Background, new ToastArguments().Add("action", "revertRemove").Add("id", autostart.Id.ToString()).ToString())
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
                    .AddText("Auto start disabled", AdaptiveTextStyle.Title)
                    .AddText(autostart.Value)
                    .AddAttributionText($"Via {autostart.Category}")
                    .AddButton("Ok", ToastActivationType.Background, new ToastArguments().Add("action", "confirmDisable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Enable", ToastActivationType.Background, new ToastArguments().Add("action", "enable").Add("id", autostart.Id.ToString()).ToString())
                    .AddButton("Remove", ToastActivationType.Background, new ToastArguments().Add("action", "revertRemove").Add("id", autostart.Id.ToString()).ToString())
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
                    .AddText("New version available", AdaptiveTextStyle.Title)
                    .AddText($"New version: {newVersion}")
                    .AddText($"Current version: {currentVersion}")
                    .AddButton("Show", ToastActivationType.Background, new ToastArguments().Add("action", "viewUpdate").ToString());
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
