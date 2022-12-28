using System;
using System.Collections.Generic;
using AutoStartConfirm.Models;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.Folder
{
    public abstract class FolderConnector : IAutoStartConnector, IDisposable, IFolderConnector
    {
        private readonly ILogger<FolderConnector> Logger;

        private IRegistryDisableService RegistryDisableService;

        public abstract string BasePath { get; }

        public abstract string DisableBasePath { get; }

        public abstract bool IsAdminRequiredForChanges(AutoStartEntry autoStart);

        private IFolderChangeMonitor FolderChangeMonitor;

        // todo: read target of links?
        // read sub directories?
        public IList<AutoStartEntry> GetCurrentAutoStarts()
        {
            Logger.LogTrace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            string[] filePaths = Directory.GetFiles(BasePath);
            foreach (var filePath in filePaths)
            {
                var fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                if (fileName.ToLower() == "desktop.ini")
                {
                    continue;
                }
                var entry = new FolderAutoStartEntry()
                {
                    Date = DateTime.Now,
                    Category = Category,
                    Path = BasePath,
                    Value = fileName,
                };
                ret.Add(entry);
            }
            return ret;
        }

        public FolderConnector(ILogger<FolderConnector> logger, IRegistryDisableService registryDisableService, IFolderChangeMonitor folderChangeMonitor)
        {
            Logger = logger;
            RegistryDisableService = registryDisableService;
            RegistryDisableService.DisableBasePath = DisableBasePath;
            RegistryDisableService.Enable += EnableHandler;
            RegistryDisableService.Disable += DisableHandler;
            FolderChangeMonitor = folderChangeMonitor;
            FolderChangeMonitor.BasePath = BasePath;
            FolderChangeMonitor.Category = Category;
            FolderChangeMonitor.Add += AddHandler;
            FolderChangeMonitor.Remove += RemoveHandler;
        }


        #region IAutoStartConnector implementation
        public abstract Category Category { get; }

        public void StartWatcher()
        {
            Logger.LogTrace("StartWatcher called for {BasePath}", BasePath);
            RegistryDisableService.StartWatcher();
            FolderChangeMonitor.Start();
            Logger.LogTrace("Watcher started");
        }

        private void RemoveHandler(AutoStartEntry e)
        {
            Logger.LogTrace("RemoveHandler called");
            Remove?.Invoke(e);
        }

        private void AddHandler(AutoStartEntry e)
        {
            Logger.LogTrace("AddHandler called");
            Add?.Invoke(e);
        }

        private void EnableHandler(string name)
        {
            Logger.LogTrace("EnableHandler called");
            var currentAutoStarts = GetCurrentAutoStarts();
            foreach (var currentAutoStart in currentAutoStarts)
            {
                if (currentAutoStart.Value == name)
                {
                    Enable?.Invoke(currentAutoStart);
                }
            }
        }

        private void DisableHandler(string name)
        {
            Logger.LogTrace("DisableHandler called");
            var currentAutoStarts = GetCurrentAutoStarts();
            foreach (var currentAutoStart in currentAutoStarts)
            {
                if (currentAutoStart.Value == name)
                {
                    Disable?.Invoke(currentAutoStart);
                }
            }
        }

        public void StopWatcher()
        {
            Logger.LogTrace("StopWatcher called for {BasePath}", BasePath);
            RegistryDisableService.StopWatcher();
            FolderChangeMonitor.Stop();
            Logger.LogTrace("Watcher stopped");
        }

        public bool CanBeAdded(AutoStartEntry autoStart)
        {
            Logger.LogTrace("CanBeAdded called for {AutoStartEntry}", autoStart);
            return false;
        }

        public bool CanBeRemoved(AutoStartEntry autoStart)
        {
            Logger.LogTrace("CanBeRemoved called for {AutoStartEntry}", autoStart);
            try
            {
                RemoveAutoStart(autoStart, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void AddAutoStart(AutoStartEntry autoStart)
        {
            Logger.LogTrace("AddAutoStart called for {AutoStartEntry}", autoStart);
            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry)
        {
            RemoveAutoStart(autoStartEntry, false);
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false)
        {
            Logger.LogTrace("RemoveAutoStart called for {AutoStartEntry} (dryRun: {DryRun})", autoStartEntry, dryRun);
            if (autoStartEntry == null)
            {
                throw new ArgumentNullException("AutoStartEntry is required");
            }
            if (autoStartEntry is FolderAutoStartEntry folderAutoStartEntry)
            {
                string fullPath = $"{folderAutoStartEntry.Path}{Path.DirectorySeparatorChar}{folderAutoStartEntry.Value}";
                if (File.Exists(fullPath))
                {
                    if (dryRun)
                    {
                        return;
                    }
                    File.Delete(fullPath);
                    Logger.LogInformation("Removed {Value} from {Path}", folderAutoStartEntry.Value, folderAutoStartEntry.Path);
                }
                else
                {
                    throw new FileNotFoundException($"File \"{fullPath}\" not found");
                }
            }
            else
            {
                throw new ArgumentException("AutoStartEntry is not of type folderAutoStartEntry");
            }
        }

        public bool CanBeEnabled(AutoStartEntry autoStart)
        {
            return RegistryDisableService.CanBeEnabled(autoStart);
        }

        public bool CanBeDisabled(AutoStartEntry autoStart)
        {
            return RegistryDisableService.CanBeDisabled(autoStart);
        }

        public void EnableAutoStart(AutoStartEntry autoStart)
        {
            RegistryDisableService.EnableAutoStart(autoStart);
        }

        public void DisableAutoStart(AutoStartEntry autoStart)
        {
            RegistryDisableService.DisableAutoStart(autoStart);
        }

        public bool IsEnabled(AutoStartEntry autoStart)
        {
            return RegistryDisableService.CanBeDisabled(autoStart);
        }


        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopWatcher();
                    FolderChangeMonitor.Add -= AddHandler;
                    FolderChangeMonitor.Remove -= RemoveHandler;
                    RegistryDisableService.Enable -= EnableHandler;
                    RegistryDisableService.Disable -= DisableHandler;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Open(AutoStartEntry autoStart)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Events
        public event AutoStartChangeHandler Add;
        public event AutoStartChangeHandler Remove;
        public event AutoStartChangeHandler Enable;
        public event AutoStartChangeHandler Disable;
        #endregion
    }
}
