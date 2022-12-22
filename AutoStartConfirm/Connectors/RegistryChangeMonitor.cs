using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;
using Windows.Devices.Geolocation;

namespace AutoStartConfirm.Connectors
{
    // source: https://erikengberg.com/4-ways-to-monitor-windows-registry-using-c/

    #region Delegates
    public delegate void RegistryChangeHandler(object sender, EventArrivedEventArgs e);
    #endregion

    public class RegistryChangeMonitor : IDisposable, IRegistryChangeMonitor
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string RegistryPath { get; }

        private ManagementEventWatcher watcher;
        private bool disposedValue;

        public RegistryChangeMonitor(string registryPath)
        {
            RegistryPath = registryPath;
        }

        private string GetQuery()
        {
            var splitted = RegistryPath.Split('\\', 2);
            var hive = splitted[0];
            var rootPath = splitted[1].Replace(@"\", @"\\");

            if (hive == "HKEY_CURRENT_USER")
            {
                // ManagementEventWatcher not supports monitoring of HKEY_CURRENT_USER
                // => Monitor current user in HKEY_USERS instead
                hive = "HKEY_USERS";
                var currentUser = WindowsIdentity.GetCurrent();
                rootPath = $"{currentUser.User.Value}\\\\{rootPath}";
            }

            return $"SELECT * FROM RegistryTreeChangeEvent " +
                $"WHERE Hive='{hive}' " +
                $"AND RootPath='{rootPath}'";
        }

        public bool Monitoring => watcher != null;

        public event RegistryChangeHandler Changed;
        public event RegistryChangeHandler Error;


        public void Start()
        {
            String query = "";
            try
            {
                if (watcher != null)
                {
                    return;
                }
                query = GetQuery();
                Logger.Debug("Query: {Query}", query);
                watcher = new ManagementEventWatcher(query);
                watcher.EventArrived +=
                    new EventArrivedEventHandler(RegistryEventHandler);
                watcher.Start();
            }
            catch (Exception ex)
            {
                var error = new Exception($"Failed to start watcher for {RegistryPath}", ex);
                if (ex is ManagementException)
                {
                    ManagementException manEx = (ManagementException)ex;
                    if (manEx.ErrorCode == ManagementStatus.NotFound)
                    {
                        Logger.Warn(error);
                    } else { 
                        Logger.Error(error);
                    }
                    return;
                }
                Logger.Error(error);
                throw error;
            }
        }

        public void Stop()
        {
            if (watcher == null)
            {
                return;
            }
            watcher.Stop();
            watcher.Dispose();
            watcher = null;
        }

        private void RegistryEventHandler(object sender, EventArrivedEventArgs e)
        {
            if (Changed != null)
            {
                Changed(this, e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~WmiRegistryEventListener()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
