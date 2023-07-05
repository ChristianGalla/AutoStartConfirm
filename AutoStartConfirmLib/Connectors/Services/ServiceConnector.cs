using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using AutoStartConfirm.Connectors.Folder;
using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;

namespace AutoStartConfirm.Connectors.Services
{
    public abstract class ServiceConnector : IAutoStartConnector, IDisposable, IServiceConnector
    {
        private readonly ILogger<ServiceConnector> Logger;

        public int WatcherIntervalInMs = 1000 * 10;

        public abstract Category Category { get; }

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart)
        {
            return true;
        }

        protected Thread? watcher = null;

        private ConcurrentDictionary<string, AutoStartEntry>? lastAutoStartEntries;

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

        public ServiceConnector(ILogger<ServiceConnector> logger)
        {
            Logger = logger;
        }


        public void Open(AutoStartEntry autoStart)
        {
            Logger.LogTrace("Open called for AutoStartEntry {AutoStartEntry}", autoStart);
            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStart)
        {
            Logger.LogTrace("RemoveAutoStart called for AutoStartEntry {AutoStartEntry}", autoStart);
            throw new NotImplementedException();
        }

        protected ServiceAutoStartEntry GetAutoStartEntry(ServiceController sc)
        {
            var newAutoStart = new ServiceAutoStartEntry()
            {
                Date = DateTime.Now,
                Category = Category,
                Value = sc.DisplayName,
                Path = sc.ServiceName,
                IsEnabled = IsEnabled(sc),
            };
            if (sc.StartType != ServiceStartMode.Disabled)
            {
                newAutoStart.EnabledStartMode = sc.StartType;
            }

            return newAutoStart;
        }

        protected static bool IsEnabled(ServiceController sc)
        {
            return sc.StartType == ServiceStartMode.Automatic ||
                sc.StartType == ServiceStartMode.Boot ||
                sc.StartType == ServiceStartMode.System;
        }

        public bool IsEnabled(AutoStartEntry autoStart)
        {
            if (autoStart is not ServiceAutoStartEntry)
            {
                throw new ArgumentException("AutoStartEntry has invalid type");
            }
            try
            {
                var sc = GetServiceController(autoStart);
                return IsEnabled(sc);
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected abstract ServiceController[] GetServiceControllers();

        protected ServiceController GetServiceController(AutoStartEntry autoStart)
        {
            var serviceControllers = GetServiceControllers();
            foreach (var sc in serviceControllers)
            {
                if (autoStart.Path == sc.ServiceName)
                {
                    return sc;
                }
            }
            throw new KeyNotFoundException($"{autoStart.Path} not found");
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts()
        {
            Logger.LogTrace("GetCurrentAutoStarts called");
            var serviceControllers = GetServiceControllers();
            var ret = new List<AutoStartEntry>();
            foreach (var sc in serviceControllers)
            {
                if (sc.StartType != ServiceStartMode.Manual)
                {
                    ServiceAutoStartEntry newAutoStart = GetAutoStartEntry(sc);
                    ret.Add(newAutoStart);
                }
            }
            return ret;
        }

        public void DisableAutoStart(AutoStartEntry autoStart)
        {
            Logger.LogTrace("DisableAutoStart called for AutoStartEntry {AutoStartEntry}", autoStart);
            if (autoStart is not ServiceAutoStartEntry)
            {
                throw new ArgumentException("AutoStartEntry must be of type ServiceAutoStartEntry");
            }
            // https://stackoverflow.com/a/35063366
            // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/changestartmode-method-in-class-win32-service?redirectedfrom=MSDN
            using var m = new ManagementObject($"Win32_Service.Name=\"{autoStart.Path}\"");
            uint returnCode = (uint)m.InvokeMethod("ChangeStartMode", new object[] { "Disabled" });
            if (returnCode != 0)
            {
                throw new Exception($"Failed to disable auto start (return code: {returnCode})");
            }
        }

        public bool CanBeRemoved(AutoStartEntry autoStart)
        {
            return false;
        }

        public bool CanBeEnabled(AutoStartEntry autoStart)
        {
            return !IsEnabled(autoStart);
        }

        public void AddAutoStart(AutoStartEntry autoStart)
        {
            throw new NotImplementedException();
        }

        public bool CanBeAdded(AutoStartEntry autoStart)
        {
            return false;
        }

        public bool CanBeDisabled(AutoStartEntry autoStart)
        {
            return IsEnabled(autoStart);
        }

        public void EnableAutoStart(AutoStartEntry autoStart)
        {
            Logger.LogTrace("EnableAutoStart called for AutoStartEntry {AutoStartEntry}", autoStart);
            if (autoStart is not ServiceAutoStartEntry)
            {
                throw new ArgumentException("AutoStartEntry must be of type ServiceAutoStartEntry");
            }
            var serviceAutoStart = (ServiceAutoStartEntry)autoStart;
            serviceAutoStart.EnabledStartMode ??= ServiceStartMode.Automatic;
            var targetMode = serviceAutoStart.EnabledStartMode!.ToString();
            using var m = new ManagementObject(string.Format("Win32_Service.Name=\"{0}\"", autoStart.Path));
            uint returnCode = (uint)m.InvokeMethod("ChangeStartMode", new object[] { targetMode! });
            if (returnCode != 0)
            {
                throw new Exception($"Failed to enable auto start (Return code: {returnCode})");
            }
        }

        private CancellationTokenSource? cancellationTokenSource;

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
                    // todo: change to event based watcher
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
                bool found = currentAutoStartsDictionary.TryGetValue(oldAutoStart.Key, out AutoStartEntry? _);
                if (!found)
                {
                    autoStartsToRemove.Add(oldAutoStart.Value);
                }
            }
            foreach (AutoStartEntry autoStartToRemove in autoStartsToRemove)
            {
                bool removed = LastAutoStartEntries.TryRemove(autoStartToRemove.Path, out AutoStartEntry? removedAutoStartEntry);
                if (removed)
                {
                    RemoveHandler(removedAutoStartEntry!);
                }
            }
            foreach (AutoStartEntry currentAutoStart in currentAutoStarts)
            {
                bool found = LastAutoStartEntries.TryGetValue(currentAutoStart.Path, out AutoStartEntry? oldAutoStart);
                if (!found)
                {
                    bool added = LastAutoStartEntries.TryAdd(currentAutoStart.Path, currentAutoStart);
                    if (added)
                    {
                        AddHandler(currentAutoStart);
                    }
                    continue;
                }
                if (oldAutoStart!.Value != currentAutoStart.Value)
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
            cancellationTokenSource?.Cancel();
            watcher.Join();
            watcher = null;
            Logger.LogTrace("Stopped watcher");
        }

        #region IDisposable Support
        private bool disposedValue = false;

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

        #endregion

        #region Events
        public event AutoStartChangeHandler? Add;
        public event AutoStartChangeHandler? Remove;
        public event AutoStartChangeHandler? Enable;
        public event AutoStartChangeHandler? Disable;
        #endregion
    }
}
