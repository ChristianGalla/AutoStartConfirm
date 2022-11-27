using System;
using System.Collections.Generic;
using AutoStartConfirm.Models;
using System.IO;

namespace AutoStartConfirm.Connectors.Folder
{
    public abstract class FolderConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IRegistryDisableService registryDisableService = null;

        private IRegistryDisableService RegistryDisableService {
            get {
                if (registryDisableService == null) {
                    registryDisableService = new RegistryDisableService(DisableBasePath);
                    registryDisableService.Enable += EnableHandler;
                    registryDisableService.Disable += DisableHandler;
                }
                return registryDisableService;
            }
        }

        public abstract string BasePath { get; }

        public abstract string DisableBasePath { get; }

        public abstract bool IsAdminRequiredForChanges(AutoStartEntry autoStart);

        private IFolderChangeMonitor monitor;

        // todo: read target of links?
        // read sub directories?
        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            string[] filePaths = Directory.GetFiles(BasePath);
            foreach (var filePath in filePaths) {
                var fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                if (fileName.ToLower() == "desktop.ini") {
                    continue;
                }
                var entry = new FolderAutoStartEntry() {
                    Date = DateTime.Now,
                    Category = Category,
                    Path = BasePath,
                    Value = fileName,
                };
                ret.Add(entry);
            }
            return ret;
        }


        #region IAutoStartConnector implementation
        public abstract Category Category { get; }

        public void StartWatcher() {
            Logger.Trace("StartWatcher called for {BasePath}", BasePath);
            RegistryDisableService.StartWatcher();
            if (monitor != null) {
                Logger.Trace("Watcher already started");
                return;
            }
            monitor = new FolderChangeMonitor() {
                BasePath = BasePath,
                Category = Category,
            };
            monitor.Add += AddHandler;
            monitor.Remove += RemoveHandler;
            Logger.Trace("Watcher started");
        }

        private void RemoveHandler(AutoStartEntry e) {
            Logger.Trace("RemoveHandler called");
            Remove?.Invoke(e);
        }

        private void AddHandler(AutoStartEntry e) {
            Logger.Trace("AddHandler called");
            Add?.Invoke(e);
        }

        private void EnableHandler(string name) {
            Logger.Trace("EnableHandler called");
            var currentAutoStarts = GetCurrentAutoStarts();
            foreach (var currentAutoStart in currentAutoStarts) {
                if (currentAutoStart.Value == name) {
                    Enable?.Invoke(currentAutoStart);
                }
            }
        }

        private void DisableHandler(string name) {
            Logger.Trace("DisableHandler called");
            var currentAutoStarts = GetCurrentAutoStarts();
            foreach (var currentAutoStart in currentAutoStarts) {
                if (currentAutoStart.Value == name) {
                    Disable?.Invoke(currentAutoStart);
                }
            }
        }

        public void StopWatcher() {
            Logger.Trace("StopWatcher called for {BasePath}", BasePath);
            RegistryDisableService.StopWatcher();
            if (monitor == null) {
                Logger.Trace("No watcher running");
                return;
            }
            monitor.Add -= AddHandler;
            monitor.Remove -= RemoveHandler;
            monitor.Stop();
            monitor.Dispose();
            monitor = null;
            Logger.Trace("Watcher stopped");
        }

        public bool CanBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("CanBeAdded called for {AutoStartEntry}", autoStart);
            return false;
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("CanBeRemoved called for {AutoStartEntry}", autoStart);
            try {
                RemoveAutoStart(autoStart, true);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("AddAutoStart called for {AutoStartEntry}", autoStart);
            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry) {
            RemoveAutoStart(autoStartEntry, false);
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false) {
            Logger.Trace("RemoveAutoStart called for {AutoStartEntry} (dryRun: {DryRun})", autoStartEntry, dryRun);
            if (autoStartEntry == null) {
                throw new ArgumentNullException("AutoStartEntry is required");
            }
            if (autoStartEntry is FolderAutoStartEntry folderAutoStartEntry) {
                string fullPath = $"{folderAutoStartEntry.Path}{Path.DirectorySeparatorChar}{folderAutoStartEntry.Value}";
                if (File.Exists(fullPath)) {
                    if (dryRun) {
                        return;
                    }
                    File.Delete(fullPath);
                    Logger.Info("Removed {Value} from {Path}", folderAutoStartEntry.Value, folderAutoStartEntry.Path);
                } else {
                    throw new FileNotFoundException($"File \"{fullPath}\" not found");
                }
            } else {
                throw new ArgumentException("AutoStartEntry is not of type folderAutoStartEntry");
            }
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            return RegistryDisableService.CanBeEnabled(autoStart);
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            return RegistryDisableService.CanBeDisabled(autoStart);
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            RegistryDisableService.EnableAutoStart(autoStart);
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            RegistryDisableService.DisableAutoStart(autoStart);
        }

        public bool IsEnabled(AutoStartEntry autoStart) {
            return RegistryDisableService.CanBeDisabled(autoStart);
        }


        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    StopWatcher();
                    if (registryDisableService != null) {
                        registryDisableService.Enable -= EnableHandler;
                        registryDisableService.Disable -= DisableHandler;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public void Open(AutoStartEntry autoStart) {
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
