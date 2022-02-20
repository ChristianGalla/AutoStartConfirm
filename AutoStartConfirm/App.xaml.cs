using AutoStartConfirm.Connectors;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Models;
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
using System.Diagnostics;
using System.Reflection;
using Windows.Foundation.Collections;
using AutoStartConfirm.GUI;
using System.Collections.ObjectModel;
using AutoStartConfirm.Properties;
using System.Collections.Specialized;
using AutoStartConfirm.Connectors.Registry;

namespace AutoStartConfirm {
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static TaskbarIcon Icon = null;

        public App() {
            // SettingsService.EnsureConfiguration();
        }

        void App_Startup(object sender, StartupEventArgs e) {
            Logger.Trace("App_Startup called"); 
            Icon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        internal static void Close() {
            Logger.Info("Closing application");
            try {
                Current.Shutdown();
            } catch (Exception e) {
                Logger.Error(e);
            }
        }

        //protected override void OnExit(ExitEventArgs e) {
        //    try {
        //        _autoStartService.SaveAutoStarts();
        //    } finally {
        //        try {
        //            Icon.Dispose();
        //        } catch (Exception) {
        //        }
        //        base.OnExit(e);
        //    }
        //}
    }
}
