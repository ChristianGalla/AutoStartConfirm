using AutoStartConfirm.AutoStartConnectors;
using AutoStartConfirm.DesktopNotifications;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace AutoStartConfirm {
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application, IDisposable {
        private string PathToLastAutoStarts = null;

        private static MainWindow Window = null;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static TaskbarIcon Icon = null;

        private AutoStartConnectorCollection Connectors = new AutoStartConnectorCollection();

        public App() {
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>("ChristianGalla.AutoStartConfirm");
            DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();
            Connectors.Add += AddHandler;
            Connectors.Remove += RemoveHandler;

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            PathToLastAutoStarts = $"{appDataPath}{Path.DirectorySeparatorChar}AutoStartConfirm{Path.DirectorySeparatorChar}LastAutoStarts.bin";
            try {
                CompareCurrentAutoStartsToLastSaved();
            } catch (Exception) {
            }
        }

        private void CompareCurrentAutoStartsToLastSaved() {
            Logger.Info("Comparing current auto starts to last saved");
            try {
                IEnumerable<AutoStartEntry> lastAutoStarts;
                try {
                    lastAutoStarts = LoadLastAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to load last auto starts", ex);
                    throw err;
                }
                IEnumerable<AutoStartEntry> currentAutoStarts;
                try {
                    currentAutoStarts = Connectors.GetCurrentAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to get current auto starts", ex);
                    throw err;
                }
                foreach (var lastAutostart in lastAutoStarts) {
                    var found = false;
                    foreach (var newAutostart in currentAutoStarts) {
                        if (newAutostart.Path == lastAutostart.Path && newAutostart.Name == lastAutostart.Name) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        RemoveHandler(lastAutostart);
                    }
                }
                foreach (var newAutostart in currentAutoStarts) {
                    var found = false;
                    foreach (var lastAutostart in lastAutoStarts) {
                        if (newAutostart.Path == lastAutostart.Path && newAutostart.Name == lastAutostart.Name) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        AddHandler(newAutostart);
                    }
                }
            } catch (Exception ex) {
                var err = new Exception("Failed to compare current auto starts to last saved", ex);
                Logger.Error(err);
                throw err;
            }
        }

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        public static void Main() {
            Logger.Info("Starting app");
            using (App app = new App()) {
                app.InitializeComponent();
                try {
                    app.Run(); // blocks until program is closing
                } catch (Exception e) {
                    Logger.Error(new Exception("Failed to run", e));
                }
                Logger.Info("Finished");
            }
        }

        void App_Startup(object sender, StartupEventArgs e) {
            Icon = (TaskbarIcon)FindResource("NotifyIcon");
            Connectors.StartWatcher();

            //Thread t1 = new Thread(new ThreadStart(StartWindow));
            //t1.Start();
        }

        public static void ToggleMainWindow() {
            Logger.Info("Toggling main window");
            if (Window == null || Window.IsClosed) {
                Logger.Trace("Creating new main window");
                Window = new MainWindow();
            }
            if (Window.IsVisible) {
                Logger.Trace("Closing main window");
                Window.Close();
            } else {
                Logger.Trace("Showing main window");
                Window.Show();
            }
        }

        public static void ShowMainWindow() {
            Logger.Info("Toggling main window");
            if (Window == null || Window.IsClosed) {
                Logger.Trace("Creating new main window");
                Window = new MainWindow();
            }
            if (!Window.IsVisible) {
                Logger.Trace("Showing main window");
                Window.Show();
            }
        }

        internal static void Close() {
            Logger.Info("Closing application");
            try {
                Current.Shutdown();
            } catch (Exception e) {
                Logger.Error(e);
            }
        }

        //public static void StartWindow()
        //{
        //    for (int i = 0; i < 10; i++)
        //    {
        //        Thread.Sleep(1000);
        //        Logger.Info(i);
        //    }
        //}

        public void SaveCurrentAutoStarts() {
            Logger.Info("Saving current auto starts");
            try {
                IEnumerable<AutoStartEntry> currentAutoStarts;
                try {
                    currentAutoStarts = Connectors.GetCurrentAutoStarts();
                } catch (Exception ex) {
                    var err = new Exception("Failed to get current auto starts", ex);
                    throw err;
                }
                try {
                    var folderPath = PathToLastAutoStarts.Substring(0, PathToLastAutoStarts.LastIndexOf(Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(folderPath);
                } catch (Exception ex) {
                    var err = new Exception("Failed to create folder", ex);
                    throw err;
                }
                try {
                    using (Stream stream = new FileStream(PathToLastAutoStarts, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, currentAutoStarts);
                    }
                } catch (Exception ex) {
                    var err = new Exception("Failed to write file", ex);
                    throw err;
                }
                Logger.Info("Saved current auto starts");
            } catch (Exception ex) {
                var err = new Exception("Failed to save current auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        public IEnumerable<AutoStartEntry> LoadLastAutoStarts() {
            Logger.Info("Loading last auto starts");
            try {
                if (!File.Exists(PathToLastAutoStarts)) {
                    return new List<AutoStartEntry>();
                }
                using (Stream stream = new FileStream(PathToLastAutoStarts, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    IFormatter formatter = new BinaryFormatter();
                    try {
                        var lastAutoStarts = (IEnumerable<AutoStartEntry>)formatter.Deserialize(stream);
                        return lastAutoStarts;
                    } catch (Exception ex) {
                        var err = new Exception("Failed to deserialize", ex);
                        throw err;
                    }
                }
            } catch (Exception ex) {
                var err = new Exception("Failed to load last auto starts", ex);
                Logger.Error(err);
                throw err;
            }
        }

        protected override void OnExit(ExitEventArgs e) {
            try {
                SaveCurrentAutoStarts();
            } finally {
                base.OnExit(e);
            }
        }


        #region Event handlers
        private void AddHandler(AutoStartEntry addedAutostart) {
            Logger.Info($"{addedAutostart.Category} autostart added: {addedAutostart.Path}\\{addedAutostart.Name}");
            ToastContent toastContent = new ToastContent() {
                // Arguments when the user taps body of toast
                Launch = "action=view&changeId=1234",

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
                            new ToastButton("Ok", "action=confirm&changeId=1234")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                            new ToastButton("Revert", "action=revert&changeId=1234")
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

        private void RemoveHandler(AutoStartEntry removedAutostart) {
            Logger.Info($"{removedAutostart.Category} autostart removed: {removedAutostart.Path}\\{removedAutostart.Name}");
            ToastContent toastContent = new ToastContent() {
                // Arguments when the user taps body of toast
                Launch = "action=view&changeId=1234",

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
                            new ToastButton("Ok", "action=confirm&changeId=1234")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                            new ToastButton("Revert", "action=revert&changeId=1234")
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
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Connectors.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
