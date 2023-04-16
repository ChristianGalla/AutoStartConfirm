using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using AutoStartConfirm.Connectors.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Windows.Devices.Geolocation;
using Windows.Web;

namespace AutoStartConfirm.Connectors
{
    // source: https://erikengberg.com/4-ways-to-monitor-windows-registry-using-c/

    #region Delegates
    public delegate void RegistryChangeHandler(object sender, EventArrivedEventArgs e);
    #endregion

    public class RegistryChangeMonitor : IDisposable, IRegistryChangeMonitor
    {
        private readonly ILogger<RegistryChangeMonitor> Logger;

        public string? RegistryPath { get; set; }

        private ManagementEventWatcher? watcher;
        private bool disposedValue = false;

        public RegistryChangeMonitor(ILogger<RegistryChangeMonitor> logger)
        {
            Logger = logger;
        }

        private string GetQuery()
        {
            if (RegistryPath == null)
            {
                throw new InvalidOperationException("RegistryPath not set");
            }
            var splitted = RegistryPath.Split('\\', 2);
            var hive = splitted[0];
            var rootPath = splitted[1].Replace(@"\", @"\\");

            if (hive == "HKEY_CURRENT_USER")
            {
                // ManagementEventWatcher not supports monitoring of HKEY_CURRENT_USER
                // => Monitor current user in HKEY_USERS instead
                hive = "HKEY_USERS";
                var currentUser = WindowsIdentity.GetCurrent();
                rootPath = $"{currentUser.User!.Value}\\\\{rootPath}";
            }

            return $"SELECT * FROM RegistryTreeChangeEvent " +
                $"WHERE Hive='{hive}' " +
                $"AND RootPath='{rootPath}'";
        }

        public bool Monitoring => watcher != null;

        public event RegistryChangeHandler? Changed;


        public void Start()
        {
            if (RegistryPath == null)
            {
                throw new InvalidOperationException("RegistryPath is not set");
            }
            String query = "";
            try
            {
                if (watcher != null)
                {
                    return;
                }
                query = GetQuery();
                Logger.LogDebug("Query: {Query}", query);
                watcher = new ManagementEventWatcher(query);
                watcher.EventArrived +=
                    new EventArrivedEventHandler(RegistryEventHandler);
                watcher.Start();
            }
            catch (Exception ex)
            {
                const string message = "Failed to start watcher for {RegistryPath}";
                if (ex is ManagementException manEx)
                {
                    if (manEx.ErrorCode == ManagementStatus.NotFound)
                    {
                        Logger.LogWarning(ex, message, RegistryPath);
                    }
                    else
                    {
                        Logger.LogError(ex, message, RegistryPath);
                    }
                    return;
                }
                Logger.LogError(ex, message, RegistryPath);
                throw new Exception($"Failed to start watcher for {RegistryPath}");
            }
        }

        public void Stop()
        {
            if (RegistryPath == null)
            {
                throw new InvalidOperationException("RegistryPath is not set");
            }
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
            Changed?.Invoke(this, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }

                disposedValue = true;
            }
        }


        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
