using AutoStartConfirm.AutoStarts;
using AutoStartConfirm.Connectors;
using System;
using System.IO;
using System.Windows;

namespace AutoStartConfirm.Helpers {
    public class FolderChangeMonitor : IDisposable, IFolderChangeMonitor {
        #region Fields
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool disposedValue;

        private FileSystemWatcher watcher;

        public string BasePath { get; set; }
        public Category Category { get; set; }
        #endregion

        #region Methods

        public void Start() {
            Logger.Trace($"Starting monitoring of {BasePath}");
            watcher = new FileSystemWatcher(BasePath) {
                NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size
            };

            // watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

        }

        public void Stop() {
            if (watcher != null) {
                Logger.Trace($"Stopping monitoring of {BasePath}");
                watcher.Dispose();
                watcher = null;
            }
        }

        // todo: filter duplicate calls (rename etc.)
        private void OnChanged(object sender, FileSystemEventArgs e) {
            if (e.ChangeType != WatcherChangeTypes.Changed ||
                e.Name.ToLower() == "desktop.ini") {
                return;
            }
            Logger.Trace($"Changed: {e.FullPath}");
            Application.Current.Dispatcher.Invoke(delegate {
                var parentDirectory = e.FullPath.Substring(0, e.FullPath.LastIndexOf("\\"));
                var removedAutostart = new FolderAutoStartEntry() {
                    Category = Category,
                    Value = e.Name,
                    Path = parentDirectory,
                    Date = DateTime.Now,
                };
                Remove?.Invoke(removedAutostart);
                var addedAutostart = new FolderAutoStartEntry() {
                    Category = Category,
                    Value = e.Name,
                    Path = parentDirectory,
                    Date = DateTime.Now,
                };
                Add?.Invoke(addedAutostart);
            });
        }

        private void OnCreated(object sender, FileSystemEventArgs e) {
            if (e.Name.ToLower() == "desktop.ini") {
                return;
            }
            Logger.Trace($"Created: {e.FullPath}");
            Application.Current.Dispatcher.Invoke(delegate {
                var parentDirectory = e.FullPath.Substring(0, e.FullPath.LastIndexOf("\\"));
                var addedAutostart = new FolderAutoStartEntry() {
                    Category = Category,
                    Value = e.Name,
                    Path = parentDirectory,
                    Date = DateTime.Now,
                };
                Add?.Invoke(addedAutostart);
            });
        }

        private void OnDeleted(object sender, FileSystemEventArgs e) {
            if (e.Name.ToLower() == "desktop.ini") {
                return;
            }
            Logger.Trace($"Deleted: {e.FullPath}");
            Application.Current.Dispatcher.Invoke(delegate {
                var parentDirectory = e.FullPath.Substring(0, e.FullPath.LastIndexOf("\\"));
                var removedAutostart = new FolderAutoStartEntry() {
                    Category = Category,
                    Value = e.Name,
                    Path = parentDirectory,
                    Date = DateTime.Now,
                };
                Remove?.Invoke(removedAutostart);
            });
        }

        private void OnRenamed(object sender, RenamedEventArgs e) {
            if (e.Name.ToLower() == "desktop.ini") {
                return;
            }
            Logger.Trace($"Renamed: {e.OldFullPath} to {e.FullPath}");
            Application.Current.Dispatcher.Invoke(delegate {
                var oldParentDirectory = e.OldFullPath.Substring(0, e.OldFullPath.LastIndexOf("\\"));
                var removedAutostart = new FolderAutoStartEntry() {
                    Category = Category,
                    Value = e.OldName,
                    Path = oldParentDirectory,
                    Date = DateTime.Now,
                };
                Remove?.Invoke(removedAutostart);
                var newParentDirectory = e.FullPath.Substring(0, e.FullPath.LastIndexOf("\\"));
                var addedAutostart = new FolderAutoStartEntry() {
                    Category = Category,
                    Value = e.Name,
                    Path = newParentDirectory,
                    Date = DateTime.Now,
                };
                Add?.Invoke(addedAutostart);
            });
        }

        private void OnError(object sender, ErrorEventArgs e) {
            Logger.Error("Error on monitoring of {BasePath}: {@Exception}", BasePath, e);
        }

        #endregion

        #region Events
        public event AutoStartChangeHandler Add;
        public event AutoStartChangeHandler Remove;
        #endregion

        #region Dispose

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                Stop();

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~IFolderChangeMonitor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
