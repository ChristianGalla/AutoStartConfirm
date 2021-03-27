using System;
using System.Collections;
using System.Collections.Generic;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.AutoStarts;
using Microsoft.Win32;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.IO;

namespace AutoStartConfirm.Connectors {
    abstract class FolderConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public abstract string BasePath { get; }

        public abstract bool IsAdminRequiredForChanges {
            get;
        }

        protected FolderChangeMonitor monitor = null;

        // todo: read target of links?
        // read sub directories?
        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            var ret = new List<AutoStartEntry>();
            string[] filePaths = Directory.GetFiles(BasePath);
            foreach (var filePath in filePaths) {
                var fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
                if (fileName.ToLower() == "desktop.ini") {
                    continue;
                }
                var entry = new FolderAutoStartEntry() {
                    Category = Category.StartMenuAutoStartFolder,
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
            StopWatcher();
            var currentAutoStarts = (List<AutoStartEntry>)GetCurrentAutoStarts();
            monitor = new FolderChangeMonitor() {
                BasePath = BasePath,
                Category = Category,
            };
            monitor.Add += AddHandler;
            monitor.Remove += RemoveHandler;
            monitor.Start();
            Logger.Trace("Watcher started");
        }

        private void RemoveHandler(AutoStartEntry e) {
            Remove?.Invoke(e);
        }

        private void AddHandler(AutoStartEntry e) {
            Add?.Invoke(e);
        }

        public void StopWatcher() {
            Logger.Trace("StopWatcher called for {BasePath}", BasePath);
            if (monitor == null) {
                Logger.Trace("No watcher running");
                return;
            }
            Logger.Trace("Stopping watcher");
            monitor.Dispose();
            monitor = null;
        }

        public bool CanBeAdded(AutoStartEntry autoStart) {
            return false;
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            return false;
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry) {
            throw new NotImplementedException();
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            return false;
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            return false;
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            throw new NotImplementedException();
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            throw new NotImplementedException();
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
        public event AutoStartChangeHandler Add;
        public event AutoStartChangeHandler Remove;
        #endregion
    }
}
