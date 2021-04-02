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

        public NotificationService() {
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>("ChristianGalla.AutoStartConfirm");
            DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();
        }

        public void ShowNewAutoStartEntryNotification(AutoStartEntry addedAutostart) {
            try {
                Logger.Trace("ShowNewAutoStartEntryNotification called for {@addedAutostart}", addedAutostart);
                ToastContent toastContent = new ToastContent() {
                    // Arguments when the user taps body of toast
                    Launch = $"action=viewAdd&id={addedAutostart.Id}",

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
                                Text = addedAutostart.Value
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
                            new ToastButton("Ok", $"action=confirmAdd&id={addedAutostart.Id}")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                            new ToastButton("Disable", $"action=disable&id={addedAutostart.Id}")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                            new ToastButton("Revert", $"action=revertAdd&id={addedAutostart.Id}")
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
                Logger.Trace("ShowNewAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show new auto start notification", e);
                Logger.Error(err);
            }
        }

        public void ShowEnabledAutoStartEntryNotification(AutoStartEntry autostart) {
            try {
                Logger.Trace("ShowEnabledAutoStartEntryNotification called for {@autostart}", autostart);
                ToastContent toastContent = new ToastContent() {
                    // Arguments when the user taps body of toast
                    Launch = $"action=viewAdd&id={autostart.Id}",

                    Visual = new ToastVisual() {
                        BindingGeneric = new ToastBindingGeneric() {
                            AppLogoOverride = new ToastGenericAppLogo() {
                                Source = $"{Directory.GetCurrentDirectory()}/Assets/AddIcon.png",
                                HintCrop = ToastGenericAppLogoCrop.None
                            },
                            Children = {
                            new AdaptiveText()
                            {
                                Text = $"Autostart enabled",
                                HintStyle = AdaptiveTextStyle.Title
                            },
                            new AdaptiveText()
                            {
                                Text = autostart.Value
                            },
                        },
                            Attribution = new ToastGenericAttributionText() {
                                Text = $"Via {autostart.Category}",
                            },
                        }
                    },
                    Actions = new ToastActionsCustom() {
                        Buttons =
                            {
                                new ToastButton("Ok", $"action=confirmEnable&id={autostart.Id}")
                                {
                                    ActivationType = ToastActivationType.Background
                                },
                                new ToastButton("Disable", $"action=disable&id={autostart.Id}")
                                {
                                    ActivationType = ToastActivationType.Background
                                },
                                new ToastButton("Remove", $"action=revertAdd&id={autostart.Id}")
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
                Logger.Trace("ShowEnabledAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show enabled auto start notification", e);
                Logger.Error(err);
            }
        }

        public void ShowRemovedAutoStartEntryNotification(AutoStartEntry removedAutostart) {
            try {
                Logger.Trace("ShowRemovedAutoStartEntryNotification called for {@removedAutostart}", removedAutostart);
                ToastContent toastContent = new ToastContent() {
                    // Arguments when the user taps body of toast
                    Launch = $"action=viewRemove&id={removedAutostart.Id}",

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
                                    Text = $"{removedAutostart.Value}"
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
                                new ToastButton("Ok", $"action=confirmRemove&id={removedAutostart.Id}")
                                {
                                    ActivationType = ToastActivationType.Background
                                },
                                new ToastButton("Revert", $"action=revertRemove&id={removedAutostart.Id}")
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
                Logger.Trace("ShowRemovedAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show removed auto start notification", e);
                Logger.Error(err);
            }
        }

        public void ShowDisabledAutoStartEntryNotification(AutoStartEntry autostart) {
            try {
                Logger.Trace("ShowDisabledAutoStartEntryNotification called for {@autostart}", autostart);
                ToastContent toastContent = new ToastContent() {
                    // Arguments when the user taps body of toast
                    Launch = $"action=viewAdd&id={autostart.Id}",

                    Visual = new ToastVisual() {
                        BindingGeneric = new ToastBindingGeneric() {
                            AppLogoOverride = new ToastGenericAppLogo() {
                                Source = $"{Directory.GetCurrentDirectory()}/Assets/RemoveIcon.png",
                                HintCrop = ToastGenericAppLogoCrop.None
                            },
                            Children = {
                                new AdaptiveText()
                                {
                                    Text = $"Autostart disabled",
                                    HintStyle = AdaptiveTextStyle.Title
                                },
                                new AdaptiveText()
                                {
                                    Text = $"{autostart.Value}"
                                }
                            },
                            Attribution = new ToastGenericAttributionText() {
                                Text = $"Via {autostart.Category}",
                            }
                        }
                    },
                    Actions = new ToastActionsCustom() {
                        Buttons =
                            {
                                new ToastButton("Ok", $"action=confirmDisable&id={autostart.Id}")
                                {
                                    ActivationType = ToastActivationType.Background
                                },
                                new ToastButton("Enable", $"action=enable&id={autostart.Id}")
                                {
                                    ActivationType = ToastActivationType.Background
                                },
                                new ToastButton("Remove", $"action=revertRemove&id={autostart.Id}")
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
                Logger.Trace("ShowDisabledAutoStartEntryNotification finished");
            } catch (Exception e) {
                var err = new Exception("Failed to show disabled auto start notification", e);
                Logger.Error(err);
            }
        }
    }
}
