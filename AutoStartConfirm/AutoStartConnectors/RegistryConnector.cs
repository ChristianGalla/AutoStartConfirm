using System;
using System.Collections;
using System.Collections.Generic;
using AutoStartConfirm.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace AutoStartConfirm.AutoStartConnectors {
    class RegistryConnector : IAutoStartConnector, IDisposable {
        public Category Category;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string basePath;

        public string[] categories;

        protected RegistryChangeMonitor monitor = null;

        protected IEnumerable<AutoStartEntry> lastAutostarts = null;

        private void ChangeHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Trace("Change detected");
            var newAutostarts = GetCurrentAutoStarts();
            var addedAutostarts = new List<AutoStartEntry>();
            var removedAutostarts = new List<AutoStartEntry>();
            foreach (var newAutostart in newAutostarts) {
                var found = false;
                foreach (var lastAutostart in lastAutostarts) {
                    if (newAutostart.Path == lastAutostart.Path && newAutostart.Name == lastAutostart.Name) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    addedAutostarts.Add(newAutostart);
                }
            }
            foreach (var lastAutostart in lastAutostarts) {
                var found = false;
                foreach (var newAutostart in newAutostarts) {
                    if (newAutostart.Path == lastAutostart.Path && newAutostart.Name == lastAutostart.Name) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    removedAutostarts.Add(lastAutostart);
                }
            }
            foreach (var addedAutostart in addedAutostarts) {
                Logger.Info($"{addedAutostart.Category} autostart added: {addedAutostart.Path}\\{addedAutostart.Name}");


                ToastContent toastContent = new ToastContent() {
                    // Arguments when the user taps body of toast
                    Launch = "action=viewConversation&conversationId=5",

                    Visual = new ToastVisual() {
                        BindingGeneric = new ToastBindingGeneric() {
                            Children = {
                                new AdaptiveText()
                                {
                                    Text = $"{addedAutostart.Category} autostart added: {addedAutostart.Path}\\{addedAutostart.Name}"
                                }
                            }
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

                Add?.Invoke(addedAutostart);
            }
            foreach (var removedAutostart in removedAutostarts) {
                Logger.Info($"{removedAutostart.Category} autostart removed: {removedAutostart.Path}\\{removedAutostart.Name}");
                Remove?.Invoke(removedAutostart);
            }
            lastAutostarts = newAutostarts;
        }

        private void ErrorHandler(object sender, RegistryChangeEventArgs e) {
            Logger.Trace("Error detected");
        }

        #region IAutoStartConnector implementation
        public IEnumerable<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("Getting current auto starts");
            try {
                var ret = new List<AutoStartEntry>();

                foreach (var category in categories) {
                    try {
                        object value = Registry.GetValue(basePath, category, null);
                        if (value == null) {
                            continue;
                        }
                        if (value is IEnumerable) {
                            foreach (var subValue in value as IEnumerable) {
                                ret.Add(new AutoStartEntry {
                                    Category = Category,
                                    Name = subValue.ToString(),
                                    Path = $"{basePath}\\{category}",
                                });
                            }
                        } else {
                            ret.Add(new AutoStartEntry {
                                Category = Category,
                                Name = value.ToString(),
                                Path = $"{basePath}\\{category}",
                            });
                        }
                    } catch (Exception ex) {
                        var err = new Exception($"Failed to get category {category}", ex);
                        throw err;
                    }
                }
                Logger.Trace("Got current auto starts");
                return ret;
            } catch (Exception ex) {
                var err = new Exception("Failed to get current auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        public void StartWatcher() {
            StopWatcher();
            lastAutostarts = GetCurrentAutoStarts();
            monitor = new RegistryChangeMonitor(basePath);
            monitor.Changed += ChangeHandler;
            monitor.Error += ErrorHandler;
            monitor.Start();
        }

        public void StopWatcher() {
            if (monitor == null) {
                return;
            }
            monitor.Dispose();
            monitor = null;
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    StopWatcher();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion

        #region Events
        public event AddHandler Add;
        public event RemoveHandler Remove;
        #endregion
    }
}
