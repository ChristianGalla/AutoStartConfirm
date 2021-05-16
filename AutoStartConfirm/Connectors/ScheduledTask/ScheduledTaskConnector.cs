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
using AutoStartConfirm.Exceptions;
using Microsoft.Win32.TaskScheduler;

namespace AutoStartConfirm.Connectors {
    class ScheduledTaskConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public bool IsAdminRequiredForChanges {
            get {
                return false; // todo
            }
        }

        protected TaskService TaskService = new TaskService();

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            var taks = TaskService.AllTasks;
            foreach (var task in taks) {
                var entry = new ScheduledTaskAutoStartEntry() {
                    AddDate = DateTime.Now,
                    Category = Category,
                    Path = task.Path,
                    Value = task.Definition.Actions.ToString(),
                };
                ret.Add(entry);
            }
            return ret;
        }


        #region IAutoStartConnector implementation
        public Category Category {
            get {
                return Category.ScheduledTask;
            }
        }

        public void StartWatcher() {
            Logger.Trace("StartWatcher called");
            StopWatcher();
            //monitor = new FolderChangeMonitor() {
            //    BasePath = BasePath,
            //    Category = Category,
            //};
            //monitor.Add += AddHandler;
            //monitor.Remove += RemoveHandler;
            //monitor.Start();
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
            //foreach(var currentAutoStart in currentAutoStarts) {
            //    if (currentAutoStart.Value == name) {
            //        Enable?.Invoke(currentAutoStart);
            //    }
            //}
        }

        private void DisableHandler(string name) {
            Logger.Trace("DisableHandler called");
            var currentAutoStarts = GetCurrentAutoStarts();
            //foreach (var currentAutoStart in currentAutoStarts) {
            //    if (currentAutoStart.Value == name) {
            //        Disable?.Invoke(currentAutoStart);
            //    }
            //}
        }

        public void StopWatcher() {
            Logger.Trace("StopWatcher called");
            //if (monitor == null) {
            //    Logger.Trace("No watcher running");
            //    return;
            //}
            Logger.Trace("Stopping watcher");
            //monitor.Dispose();
            //monitor = null;
        }

        public bool CanBeAdded(AutoStartEntry autoStart) {
            Logger.Trace("CanBeAdded called for {autoStart}", autoStart);
            return false;
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            Logger.Trace("CanBeRemoved called for {autoStart}", autoStart);
            try {
                RemoveAutoStart(autoStart, true);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("AddAutoStart called for {autoStart}", autoStart);
            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry) {
            RemoveAutoStart(autoStartEntry, false);
        }

        public void RemoveAutoStart(AutoStartEntry autoStartEntry, bool dryRun = false) {
            Logger.Trace("RemoveAutoStart called for {autoStartEntry} (dryRun: {DryRun})", autoStartEntry, dryRun);
            if (autoStartEntry == null) {
                throw new ArgumentNullException("autoStartEntry is required");
            }
            if (autoStartEntry is ScheduledTaskAutoStartEntry ScheduledTaskAutoStartEntry) {
                // string fullPath = $"{ScheduledTaskAutoStartEntry.Path}{Path.DirectorySeparatorChar}{ScheduledTaskAutoStartEntry.Value}";
                //if (File.Exists(fullPath)) {
                //    if (dryRun) {
                //        return;
                //    }
                //    File.Delete(fullPath);
                //    Logger.Info("Removed {Value} from {Path}", ScheduledTaskAutoStartEntry.Value, ScheduledTaskAutoStartEntry.Path);
                //} else {
                //    throw new FileNotFoundException($"File \"{fullPath}\" not found");
                //}
            } else {
                throw new ArgumentException("autoStartEntry is not of type ScheduledTaskAutoStartEntry");
            }
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            var task = TaskService.GetTask(autoStart.Path);
            if (task == null) {
                return false;
            }
            return !task.Enabled;
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            var task = TaskService.GetTask(autoStart.Path);
            if (task == null) {
                return false;
            }
            return task.Enabled;
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            var task = TaskService.GetTask(autoStart.Path);
            task.Enabled = true;
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            var task = TaskService.GetTask(autoStart.Path);
            task.Enabled = false;
        }

        public bool IsEnabled(AutoStartEntry autoStart) {
            var task = TaskService.GetTask(autoStart.Path);
            if (task == null) {
                return false;
            }
            return task.Enabled;
        }


        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    StopWatcher();
                    TaskService.Dispose();
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
        public event AutoStartChangeHandler Enable;
        public event AutoStartChangeHandler Disable;
        #endregion
    }
}
