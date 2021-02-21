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

namespace AutoStartConfirm.Connectors {
    abstract class FolderConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public abstract string BasePath { get; }

        public abstract bool IsAdminRequiredForChanges {
            get;
        }

        protected FolderChangeMonitor monitor = null;

        protected List<FolderAutoStartEntry> lastAutostarts = null;

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            throw new NotImplementedException();
        }


        #region IAutoStartConnector implementation
        public abstract Category Category { get; }

        public void StartWatcher() {
            Logger.Trace("StartWatcher called for {BasePath}", BasePath);
            StopWatcher();
            var currentAutoStarts = (List<AutoStartEntry>)GetCurrentAutoStarts();
            lastAutostarts = new List<FolderAutoStartEntry>();
            foreach (var currentAutoStart in currentAutoStarts) {
                lastAutostarts.Add((FolderAutoStartEntry)currentAutoStart);
            }
            throw new NotImplementedException();
            //monitor = new FolderChangeMonitor(BasePath);
            //monitor.Changed += ChangeHandler;
            //monitor.Error += ErrorHandler;
            //monitor.Start();
            //Logger.Trace("Watcher started");
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

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("AddAutoStart called for {Value} in {Path}", autoStart.Value, autoStart.Path);
            if (!(autoStart is FolderAutoStartEntry)) {
                throw new ArgumentException("Parameter must be of type FolderAutoStartEntry");
            }
            FolderAutoStartEntry regAutoStart = (FolderAutoStartEntry)autoStart;

            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry) {
            Logger.Trace("RemoveAutoStart called for {Value} in {Path}", autoStartEntry.Value, autoStartEntry.Path);
            if (!(autoStartEntry is FolderAutoStartEntry)) {
                throw new ArgumentException("Parameter must be of type FolderAutoStartEntry");
            }
            // @todo
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
