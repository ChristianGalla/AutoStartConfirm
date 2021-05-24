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
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace AutoStartConfirm.Connectors {
    class ScheduledTaskConnector : IAutoStartConnector, IDisposable {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public int WatcherIntervalInMs = 1000 * 60;

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            return true;
        }

        protected Thread watcherThread = null;

        private ConcurrentDictionary<string, AutoStartEntry> lastAutoStartEntries;

        protected ConcurrentDictionary<string, AutoStartEntry> LastAutoStartEntries {
            get {
                if (lastAutoStartEntries == null) {
                    var currentAutoStarts = GetCurrentAutoStarts();
                    lastAutoStartEntries = new ConcurrentDictionary<string, AutoStartEntry>(1, currentAutoStarts.Count);
                    foreach (AutoStartEntry autoStart in currentAutoStarts) {
                        LastAutoStartEntries[autoStart.Path] = autoStart;
                    }
                }
                return lastAutoStartEntries;
            }
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            using (var taskService = TaskService.Instance) {
                var tasks = taskService.AllTasks;
                foreach (var task in tasks) {
                    try {
                        ScheduledTaskAutoStartEntry entry = GetAutoStartEntry(task);
                        ret.Add(entry);
                    } catch (Exception ex) {
                        string path = "";
                        try {
                            path = $" {task.Path}";
                        } catch (Exception) {
                        }
                        Logger.Error(new Exception($"Failed to get details of scheduled task {path}", ex));
                    }
                }
                return ret;
            }
        }

        private ScheduledTaskAutoStartEntry GetAutoStartEntry(Microsoft.Win32.TaskScheduler.Task task) {
            return new ScheduledTaskAutoStartEntry() {
                AddDate = DateTime.Now,
                Category = Category,
                Path = task.Path,
                Value = task.Definition.Actions.ToString(),
                IsEnabled = task.Enabled,
            };
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

            // Thread is created manually because
            // thread pool should not be used for long running tasks
            // https://docs.microsoft.com/en-us/dotnet/standard/threading/the-managed-thread-pool#when-not-to-use-thread-pool-threads
            watcherThread = new Thread(new ThreadStart(() => {
                while (true) {
                    Thread.Sleep(WatcherIntervalInMs);
                    CheckChanges();
                }
            })) {
                Priority = ThreadPriority.Lowest
            };
            watcherThread.Start();
            Logger.Trace("Watcher started");
        }

        private void CheckChanges() {
            Logger.Trace("CheckChanges called");
            var currentAutoStarts = GetCurrentAutoStarts();
            var currentAutoStartsDictionary = new Dictionary<string, AutoStartEntry>();
            foreach (AutoStartEntry currentAutoStart in currentAutoStarts) {
                currentAutoStartsDictionary[currentAutoStart.Path] = currentAutoStart;
            }
            var autoStartsToRemove = new List<AutoStartEntry>();
            foreach (var oldAutoStart in LastAutoStartEntries) {
                bool found = currentAutoStartsDictionary.TryGetValue(oldAutoStart.Key, out AutoStartEntry newAutoStartEntry);
                if (!found) {
                    autoStartsToRemove.Add(oldAutoStart.Value);
                }
            }
            foreach (AutoStartEntry autoStartToRemove in autoStartsToRemove) {
                bool removed = LastAutoStartEntries.TryRemove(autoStartToRemove.Path, out AutoStartEntry removedAutoStartEntry);
                if (removed) {
                    RemoveHandler(removedAutoStartEntry);
                }
            }
            foreach (AutoStartEntry currentAutoStart in currentAutoStarts) {
                bool found = LastAutoStartEntries.TryGetValue(currentAutoStart.Path, out AutoStartEntry oldAutoStart);
                if (!found) {
                    bool added = LastAutoStartEntries.TryAdd(currentAutoStart.Path, currentAutoStart);
                    if (added) {
                        AddHandler(currentAutoStart);
                    }
                    continue;
                }
                if (oldAutoStart.Value != currentAutoStart.Value) {
                    bool updated = LastAutoStartEntries.TryUpdate(currentAutoStart.Path, currentAutoStart, oldAutoStart);
                    if (updated) {
                        RemoveHandler(oldAutoStart);
                        AddHandler(currentAutoStart);
                    }
                    continue;
                }
                bool wasEnabled = oldAutoStart.IsEnabled.GetValueOrDefault(true);
                bool nowEnabled = currentAutoStart.IsEnabled.GetValueOrDefault(true);
                if (wasEnabled != nowEnabled) {
                    oldAutoStart.IsEnabled = nowEnabled;
                    if (nowEnabled) {
                        EnableHandler(currentAutoStart);
                    } else {
                        DisableHandler(currentAutoStart);
                    }
                }
            }
        }

        private void RemoveHandler(AutoStartEntry e) {
            Logger.Trace("RemoveHandler called");
            Remove?.Invoke(e);
        }

        private void AddHandler(AutoStartEntry e) {
            Logger.Trace("AddHandler called");
            Add?.Invoke(e);
        }

        private void EnableHandler(AutoStartEntry e) {
            Logger.Trace("EnableHandler called");
            Enable?.Invoke(e);
        }

        private void DisableHandler(AutoStartEntry e) {
            Logger.Trace("DisableHandler called");
            Disable?.Invoke(e);
        }

        public void StopWatcher() {
            Logger.Trace("StopWatcher called");
            if (watcherThread == null) {
                Logger.Trace("No watcher running");
                return;
            }
            Logger.Trace("Stopping watcher");
            watcherThread.Abort();
            watcherThread.Join();
            watcherThread = null;
            Logger.Trace("Stopped watcher");
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
            if (autoStartEntry is ScheduledTaskAutoStartEntry ScheduledTaskAutoStartEntry) {
                var task = TaskService.Instance.GetTask(autoStartEntry.Path);
                if (task != null) {
                    if (dryRun) {
                        return;
                    }
                    task.Folder.DeleteTask(task.Name);
                    Logger.Info("Removed {Value} from {Path}", ScheduledTaskAutoStartEntry.Value, ScheduledTaskAutoStartEntry.Path);
                    bool removed = lastAutoStartEntries.TryRemove(ScheduledTaskAutoStartEntry.Path, out AutoStartEntry removedAutoStart);
                    if (removed) {
                        RemoveHandler(removedAutoStart);
                    }
                } else {
                    throw new ArgumentException("Task not found");
                }
            } else {
                throw new ArgumentException("AutoStartEntry is not of type ScheduledTaskAutoStartEntry");
            }
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            var task = TaskService.Instance.GetTask(autoStart.Path);
            if (task == null) {
                return false;
            }
            return !task.Enabled;
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            var task = TaskService.Instance.GetTask(autoStart.Path);
            if (task == null) {
                return false;
            }
            return task.Enabled;
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            ToggleEnable(autoStart, true);
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            ToggleEnable(autoStart, false);
        }

        private void ToggleEnable(AutoStartEntry autoStart, bool enable) {
            var task = TaskService.Instance.GetTask(autoStart.Path);
            if (task == null) {
                throw new InvalidOperationException($"Task {autoStart.Path} not found");
            }
            if (task.Enabled == enable) {
                return;
            }
            task.Enabled = enable;
            var currentAutoStart = GetAutoStartEntry(task);
            LastAutoStartEntries.AddOrUpdate(
                autoStart.Path,
                (key) => {
                    return currentAutoStart;
                },
                (key, oldValue) => {
                    return currentAutoStart;
                }
            );
            if (enable) {
                EnableHandler(currentAutoStart);
            } else {
                DisableHandler(currentAutoStart);
            }
        }

        public bool IsEnabled(AutoStartEntry autoStart) {
            var task = TaskService.Instance.GetTask(autoStart.Path);
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
