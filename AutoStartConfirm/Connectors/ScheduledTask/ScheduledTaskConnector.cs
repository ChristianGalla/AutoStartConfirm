using System;
using System.Collections.Generic;
using AutoStartConfirm.Models;
using Microsoft.Win32.TaskScheduler;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.ScheduledTask
{
    public class ScheduledTaskConnector : IAutoStartConnector, IDisposable, IScheduledTaskConnector
    {
        private readonly ILogger<ScheduledTaskConnector> Logger;

        public int WatcherIntervalInMs = 1000 * 10;

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart)
        {
            return true;
        }

        protected Thread watcher = null;

        private ConcurrentDictionary<string, AutoStartEntry> lastAutoStartEntries;

        protected ConcurrentDictionary<string, AutoStartEntry> LastAutoStartEntries
        {
            get
            {
                if (lastAutoStartEntries == null)
                {
                    var currentAutoStarts = GetCurrentAutoStarts();
                    lastAutoStartEntries = new ConcurrentDictionary<string, AutoStartEntry>(1, currentAutoStarts.Count);
                    foreach (AutoStartEntry autoStart in currentAutoStarts)
                    {
                        LastAutoStartEntries[autoStart.Path] = autoStart;
                    }
                }
                return lastAutoStartEntries;
            }
        }

        public ScheduledTaskConnector(ILogger<ScheduledTaskConnector> logger)
        {
            Logger = logger;
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts()
        {
            Logger.LogTrace("GetCurrentAutoStarts called");
            var ret = new List<AutoStartEntry>();
            using (var taskService = TaskService.Instance)
            {
                var tasks = taskService.AllTasks;
                foreach (var task in tasks)
                {
                    try
                    {
                        ScheduledTaskAutoStartEntry entry = GetAutoStartEntry(task);
                        ret.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        string path = "";
                        try
                        {
                            path = $" {task.Path}";
                        }
                        catch (Exception)
                        {
                        }
                        Logger.LogError(ex, "Failed to get details of scheduled task {path}", path);
                    }
                }
                return ret;
            }
        }

        private ScheduledTaskAutoStartEntry GetAutoStartEntry(Microsoft.Win32.TaskScheduler.Task task)
        {
            return new ScheduledTaskAutoStartEntry()
            {
                Date = DateTime.Now,
                Category = Category,
                Path = task.Path,
                Value = task.Definition.Actions.ToString(),
                IsEnabled = task.Enabled,
            };
        }


        #region IAutoStartConnector implementation
        public Category Category
        {
            get
            {
                return Category.ScheduledTask;
            }
        }

        private CancellationTokenSource cancellationTokenSource;

        public void StartWatcher()
        {
            Logger.LogTrace("StartWatcher called");
            if (watcher != null)
            {
                Logger.LogTrace("Watcher already running");
                return;
            }

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            // Thread is created manually because
            // thread pool should not be used for long running tasks
            // https://docs.microsoft.com/en-us/dotnet/standard/threading/the-managed-thread-pool#when-not-to-use-thread-pool-threads
            watcher = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    token.WaitHandle.WaitOne(WatcherIntervalInMs);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    CheckChanges();
                }
            }))
            {
                Priority = ThreadPriority.Lowest,
                IsBackground = true,
                Name = $"{Category} watcher",
            };
            watcher.Start();
            Logger.LogTrace("Watcher started");
        }

        private void CheckChanges()
        {
            Logger.LogTrace("CheckChanges called");
            var currentAutoStarts = GetCurrentAutoStarts();
            var currentAutoStartsDictionary = new Dictionary<string, AutoStartEntry>();
            foreach (AutoStartEntry currentAutoStart in currentAutoStarts)
            {
                currentAutoStartsDictionary[currentAutoStart.Path] = currentAutoStart;
            }
            var autoStartsToRemove = new List<AutoStartEntry>();
            foreach (var oldAutoStart in LastAutoStartEntries)
            {
                bool found = currentAutoStartsDictionary.TryGetValue(oldAutoStart.Key, out _);
                if (!found)
                {
                    autoStartsToRemove.Add(oldAutoStart.Value);
                }
            }
            foreach (AutoStartEntry autoStartToRemove in autoStartsToRemove)
            {
                bool removed = LastAutoStartEntries.TryRemove(autoStartToRemove.Path, out AutoStartEntry removedAutoStartEntry);
                if (removed)
                {
                    RemoveHandler(removedAutoStartEntry);
                }
            }
            foreach (AutoStartEntry currentAutoStart in currentAutoStarts)
            {
                bool found = LastAutoStartEntries.TryGetValue(currentAutoStart.Path, out AutoStartEntry oldAutoStart);
                if (!found)
                {
                    bool added = LastAutoStartEntries.TryAdd(currentAutoStart.Path, currentAutoStart);
                    if (added)
                    {
                        AddHandler(currentAutoStart);
                    }
                    continue;
                }
                if (oldAutoStart.Value != currentAutoStart.Value)
                {
                    bool updated = LastAutoStartEntries.TryUpdate(currentAutoStart.Path, currentAutoStart, oldAutoStart);
                    if (updated)
                    {
                        RemoveHandler(oldAutoStart);
                        AddHandler(currentAutoStart);
                    }
                    continue;
                }
                bool wasEnabled = oldAutoStart.IsEnabled.GetValueOrDefault(true);
                bool nowEnabled = currentAutoStart.IsEnabled.GetValueOrDefault(true);
                if (wasEnabled != nowEnabled)
                {
                    oldAutoStart.IsEnabled = nowEnabled;
                    if (nowEnabled)
                    {
                        EnableHandler(currentAutoStart);
                    }
                    else
                    {
                        DisableHandler(currentAutoStart);
                    }
                }
            }
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

        private void EnableHandler(AutoStartEntry e)
        {
            Logger.LogTrace("EnableHandler called");
            Enable?.Invoke(e);
        }

        private void DisableHandler(AutoStartEntry e)
        {
            Logger.LogTrace("DisableHandler called");
            Disable?.Invoke(e);
        }

        public void StopWatcher()
        {
            Logger.LogTrace("StopWatcher called");
            if (watcher == null)
            {
                Logger.LogTrace("No watcher running");
                return;
            }
            Logger.LogTrace("Stopping watcher");
            cancellationTokenSource.Cancel();
            watcher.Join();
            watcher = null;
            Logger.LogTrace("Stopped watcher");
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
                throw new ArgumentNullException(nameof(autoStartEntry));
            }
            if (autoStartEntry is ScheduledTaskAutoStartEntry ScheduledTaskAutoStartEntry)
            {
                var task = TaskService.Instance.GetTask(autoStartEntry.Path);
                if (task != null)
                {
                    if (dryRun)
                    {
                        return;
                    }
                    task.Folder.DeleteTask(task.Name);
                    Logger.LogInformation("Removed {Value} from {Path}", ScheduledTaskAutoStartEntry.Value, ScheduledTaskAutoStartEntry.Path);
                    bool removed = lastAutoStartEntries.TryRemove(ScheduledTaskAutoStartEntry.Path, out AutoStartEntry removedAutoStart);
                    if (removed)
                    {
                        RemoveHandler(removedAutoStart);
                    }
                }
                else
                {
                    throw new ArgumentException("Task not found");
                }
            }
            else
            {
                throw new ArgumentException("AutoStartEntry is not of type ScheduledTaskAutoStartEntry");
            }
        }

        public bool CanBeEnabled(AutoStartEntry autoStart)
        {
            var task = TaskService.Instance.GetTask(autoStart.Path);
            if (task == null)
            {
                return false;
            }
            return !task.Enabled;
        }

        public bool CanBeDisabled(AutoStartEntry autoStart)
        {
            var task = TaskService.Instance.GetTask(autoStart.Path);
            if (task == null)
            {
                return false;
            }
            return task.Enabled;
        }

        public void EnableAutoStart(AutoStartEntry autoStart)
        {
            ToggleEnable(autoStart, true);
        }

        public void DisableAutoStart(AutoStartEntry autoStart)
        {
            ToggleEnable(autoStart, false);
        }

        private void ToggleEnable(AutoStartEntry autoStart, bool enable)
        {
            var task = TaskService.Instance.GetTask(autoStart.Path);
            if (task == null)
            {
                throw new InvalidOperationException($"Task {autoStart.Path} not found");
            }
            if (task.Enabled == enable)
            {
                return;
            }
            task.Enabled = enable;
            var currentAutoStart = GetAutoStartEntry(task);
            LastAutoStartEntries.AddOrUpdate(
                autoStart.Path,
                (key) =>
                {
                    return currentAutoStart;
                },
                (key, oldValue) =>
                {
                    return currentAutoStart;
                }
            );
            if (enable)
            {
                EnableHandler(currentAutoStart);
            }
            else
            {
                DisableHandler(currentAutoStart);
            }
        }

        public bool IsEnabled(AutoStartEntry autoStart)
        {
            var task = TaskService.Instance.GetTask(autoStart.Path);
            if (task == null)
            {
                return false;
            }
            return task.Enabled;
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
                    cancellationTokenSource?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
