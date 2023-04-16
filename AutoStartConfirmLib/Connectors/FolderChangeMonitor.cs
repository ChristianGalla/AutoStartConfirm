using AutoStartConfirm.Connectors.Folder;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows;

namespace AutoStartConfirm.Connectors
{
    public class FolderChangeMonitor : IDisposable, IFolderChangeMonitor {
        #region Fields
        private readonly ILogger<FolderChangeMonitor> Logger;
        private readonly IDispatchService DispatchService;

        private bool disposedValue;

        private FileSystemWatcher? watcher;

        public required string BasePath { get; set; }
        public Category Category { get; set; }
        #endregion

        #region Methods

        public FolderChangeMonitor(
            ILogger<FolderChangeMonitor> logger,
            IDispatchService dispatchService)
        {
            Logger = logger;
            DispatchService = dispatchService;
        }

        public void Start() {
            Logger.LogTrace("Starting monitoring of {BasePath}", BasePath);
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
                Logger.LogTrace("Stopping monitoring of {BasePath}", BasePath);
                watcher.Created -= OnCreated;
                watcher.Deleted -= OnDeleted;
                watcher.Renamed -= OnRenamed;
                watcher.Error -= OnError;
                watcher.Dispose();
                watcher = null;
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.Name == null)
            {
                return;
            }
            if (e.Name.ToLower() == "desktop.ini") {
                return;
            }
            Logger.LogTrace("Created: {FullPath}", e.FullPath);
            DispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                var parentDirectory = e.FullPath[..e.FullPath.LastIndexOf("\\")];
                var addedAutostart = new FolderAutoStartEntry()
                {
                    Category = Category,
                    Value = e.Name,
                    Path = parentDirectory,
                    Date = DateTime.Now,
                };
                Add?.Invoke(addedAutostart);
            });
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.Name == null)
            {
                return;
            }
            if (e.Name.ToLower() == "desktop.ini") {
                return;
            }
            Logger.LogTrace("Deleted: {FullPath}", e.FullPath);
            DispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                var parentDirectory = e.FullPath[..e.FullPath.LastIndexOf("\\")];
                var removedAutostart = new FolderAutoStartEntry()
                {
                    Category = Category,
                    Value = e.Name,
                    Path = parentDirectory,
                    Date = DateTime.Now,
                };
                Remove?.Invoke(removedAutostart);
            });
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (e.Name == null)
            {
                return;
            }
            if (e.Name.ToLower() == "desktop.ini") {
                return;
            }
            Logger.LogTrace("Renamed: {OldFullPath} to {FullPath}", e.OldFullPath, e.FullPath);
            DispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                if (e.OldName != null)
                {
                    var oldParentDirectory = e.OldFullPath[..e.OldFullPath.LastIndexOf("\\")];
                    var removedAutostart = new FolderAutoStartEntry()
                    {
                        Category = Category,
                        Value = e.OldName,
                        Path = oldParentDirectory,
                        Date = DateTime.Now,
                    };
                    Remove?.Invoke(removedAutostart);
                }
                var newParentDirectory = e.FullPath[..e.FullPath.LastIndexOf("\\")];
                var addedAutostart = new FolderAutoStartEntry()
                {
                    Category = Category,
                    Value = e.Name,
                    Path = newParentDirectory,
                    Date = DateTime.Now,
                };
                Add?.Invoke(addedAutostart);
            });
        }

        private void OnError(object sender, ErrorEventArgs e) {
            Logger.LogError("Error on monitoring of {BasePath}: {@Exception}", BasePath, e);
        }

        #endregion

        #region Events
        public event AutoStartChangeHandler? Add;
        public event AutoStartChangeHandler? Remove;
        #endregion

        #region Dispose

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing)
                {
                    Stop();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
