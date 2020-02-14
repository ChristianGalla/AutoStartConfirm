using AutoStartConfirm.AutoStartConnectors;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoStartConfirm
{
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private static MainWindow Window = null;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static TaskbarIcon Icon = null;

        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.STAThreadAttribute()]
        public static void Main()
        {
            Logger.Info("Starting app");
            using (var connector = new BootExecuteConnector())
            {
                // var currentAutoStarts = connector.GetCurrentAutoStarts();
                connector.StartWartcher();
                AutoStartConfirm.App app = new AutoStartConfirm.App();
                app.InitializeComponent();
                try
                {
                    app.Run();
                }
                catch (Exception e)
                {
                    Logger.Error(new Exception("Failed to run", e));
                }
            }

            Logger.Info("Finished");
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            Icon = (TaskbarIcon)FindResource("NotifyIcon");

            //Thread t1 = new Thread(new ThreadStart(StartWindow));
            //t1.Start();
        }

        public static void ToggleMainWindow()
        {
            Logger.Info("Toggling main window");
            if (Window == null || Window.IsClosed)
            {
                Logger.Trace("Creating new main window");
                Window = new MainWindow();
            }
            if (Window.IsVisible)
            {
                Logger.Trace("Closing main window");
                Window.Close();
            }
            else
            {
                Logger.Trace("Showing main window");
                Window.Show();
            }
        }

        internal static void Close()
        {
            Logger.Info("Closing application");
            try
            {
                Application.Current.Shutdown();
            } catch (Exception e)
            {
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
    }
}
