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
    public class NotificationService {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public NotificationService () {
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>("ChristianGalla.AutoStartConfirm");
            DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();
        }

        public void ShowNewAutoStartEntryNotification(AutoStartEntry addedAutostart) {
            Logger.Info($"{addedAutostart.Category} autostart added: {addedAutostart.Path}\\{addedAutostart.Name}");
            ToastContent toastContent = new ToastContent() {
                // Arguments when the user taps body of toast
                Launch = $"action=view&id={addedAutostart.Id}",

                Visual = new ToastVisual() {
                    BindingGeneric = new ToastBindingGeneric() {
                        AppLogoOverride = new ToastGenericAppLogo() {
                            Source = $"{Directory.GetCurrentDirectory()}/Assets/AddIcon.png",
                            HintCrop = ToastGenericAppLogoCrop.None
                        },
                        Children = {
                            new AdaptiveText()
                            {
                                Text = $"Autostart added",
                                HintStyle = AdaptiveTextStyle.Title
                            },
                            new AdaptiveText()
                            {
                                Text = addedAutostart.Name
                            },
                        },
                        Attribution = new ToastGenericAttributionText() {
                            Text = $"Via {addedAutostart.Category}",
                        },
                    }
                },
                Actions = new ToastActionsCustom() {
                    Buttons =
                        {
                            new ToastButton("Ok", $"action=confirm&id={addedAutostart.Id}")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                            new ToastButton("Revert", $"action=revert&id={addedAutostart.Id}")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                        }
                }
            };

            // Create the XML document (BE SURE TO REFERENCE WINDOWS.DATA.XML.DOM)
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc);

            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }

        public void ShowRemovedAutoStartEntryNotification(AutoStartEntry removedAutostart) {
            Logger.Info($"{removedAutostart.Category} autostart removed: {removedAutostart.Path}\\{removedAutostart.Name}");
            ToastContent toastContent = new ToastContent() {
                // Arguments when the user taps body of toast
                Launch = $"action=view&id={removedAutostart.Id}",

                Visual = new ToastVisual() {
                    BindingGeneric = new ToastBindingGeneric() {
                        AppLogoOverride = new ToastGenericAppLogo() {
                            Source = $"{Directory.GetCurrentDirectory()}/Assets/RemoveIcon.png",
                            HintCrop = ToastGenericAppLogoCrop.None
                        },
                        Children = {
                            new AdaptiveText()
                            {
                                Text = $"Autostart removed",
                                HintStyle = AdaptiveTextStyle.Title
                            },
                            new AdaptiveText()
                            {
                                Text = $"{removedAutostart.Name}"
                            }
                        },
                        Attribution = new ToastGenericAttributionText() {
                            Text = $"Via {removedAutostart.Category}",
                        }
                    }
                },
                Actions = new ToastActionsCustom() {
                    Buttons =
                        {
                            new ToastButton("Ok", $"action=confirm&id={removedAutostart.Id}")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                            new ToastButton("Revert", $"action=revert&id={removedAutostart.Id}")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                        }
                }
            };

            // Create the XML document (BE SURE TO REFERENCE WINDOWS.DATA.XML.DOM)
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc);

            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }
    }
}
