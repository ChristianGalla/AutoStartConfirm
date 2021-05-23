using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using AutoStartConfirm.AutoStarts;

namespace AutoStartConfirm.Connectors {
    abstract class ServiceConnector : IAutoStartConnector, IDisposable {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // public int WatcherIntervalInMs = 1000 * 60;
        public int WatcherIntervalInMs = 1000 * 10;

        public abstract Category Category { get; }

        public bool IsAdminRequiredForChanges(AutoStartEntry autoStart) {
            return true;
        }

        protected CancellationTokenSource MonitorCancellationTokenSource;

        protected System.Threading.Tasks.Task MonitorTask;

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


        public void Open(AutoStartEntry autoStart) {
            Logger.Trace("Open called for AutoStartEntry {AutoStartEntry}", autoStart);
            throw new NotImplementedException();
        }

        public void RemoveAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("RemoveAutoStart called for AutoStartEntry {AutoStartEntry}", autoStart);
            throw new NotImplementedException();
        }

        protected ServiceAutoStartEntry GetAutoStartEntry(ServiceController sc) {
            Logger.Trace("GetAutoStartEntry called for service controller {ServiceController}", sc);
            var newAutoStart = new ServiceAutoStartEntry() {
                AddDate = DateTime.Now,
                Category = Category,
                Value = sc.DisplayName,
                Path = sc.ServiceName,
            };
            newAutoStart.IsEnabled = IsEnabled(sc);
            if (sc.StartType != ServiceStartMode.Disabled) {
                newAutoStart.EnabledStartMode = sc.StartType;
            }

            return newAutoStart;
        }

        protected bool IsEnabled(ServiceController sc) {
            Logger.Trace("IsEnabled called for ServiceController {ServiceController}", sc);
            return sc.StartType == ServiceStartMode.Automatic ||
                sc.StartType == ServiceStartMode.Boot ||
                sc.StartType == ServiceStartMode.System;
        }

        public bool IsEnabled(AutoStartEntry autoStart) {
            Logger.Trace("IsEnabled called for AutoStartEntry {AutoStartEntry}", autoStart);
            if (!(autoStart is ServiceAutoStartEntry)) {
                throw new ArgumentException("AutoStartEntry has invalid type");
            }
            try {
                var sc = GetServiceController(autoStart);
                return IsEnabled(sc);
            } catch (Exception) {
                return false;
            }
        }

        protected abstract ServiceController[] GetServiceControllers();

        protected ServiceController GetServiceController(AutoStartEntry autoStart) {
            Logger.Trace("GetServiceController called for AutoStartEntry {AutoStartEntry}", autoStart);
            var serviceControllers = GetServiceControllers();
            foreach (var sc in serviceControllers) {
                if (autoStart.Path == sc.ServiceName) {
                    return sc;
                }
            }
            throw new KeyNotFoundException($"{autoStart.Path} not found");
        }

        public IList<AutoStartEntry> GetCurrentAutoStarts() {
            Logger.Trace("GetCurrentAutoStarts called");
            var serviceControllers = GetServiceControllers();
            var ret = new List<AutoStartEntry>();
            foreach (var sc in serviceControllers) {
                ServiceAutoStartEntry newAutoStart = GetAutoStartEntry(sc);
                ret.Add(newAutoStart);
            }
            return ret;
        }

        public void DisableAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("DisableAutoStart called for AutoStartEntry {AutoStartEntry}", autoStart);
            if (!(autoStart is ServiceAutoStartEntry)) {
                throw new ArgumentException("AutoStartEntry must be of type ServiceAutoStartEntry");
            }
            // https://stackoverflow.com/a/35063366
            // https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/changestartmode-method-in-class-win32-service?redirectedfrom=MSDN
            using (var m = new ManagementObject(string.Format("Win32_Service.Name=\"{0}\"", autoStart.Path))) {
                uint returnCode = (uint)m.InvokeMethod("ChangeStartMode", new object[] { "Disabled" });
                if (returnCode != 0) {
                    throw new Exception($"Failed to disable auto start (return code: {returnCode})");
                }
            }
        }

        public bool CanBeRemoved(AutoStartEntry autoStart) {
            return false;
        }

        public bool CanBeEnabled(AutoStartEntry autoStart) {
            return !IsEnabled(autoStart);
        }

        public void AddAutoStart(AutoStartEntry autoStart) {
            throw new NotImplementedException();
        }

        public bool CanBeAdded(AutoStartEntry autoStart) {
            return false;
        }

        public bool CanBeDisabled(AutoStartEntry autoStart) {
            return IsEnabled(autoStart);
        }

        public void EnableAutoStart(AutoStartEntry autoStart) {
            Logger.Trace("EnableAutoStart called for AutoStartEntry {AutoStartEntry}", autoStart);
            if (!(autoStart is ServiceAutoStartEntry)) {
                throw new ArgumentException("AutoStartEntry must be of type ServiceAutoStartEntry");
            }
            var serviceAutoStart = (ServiceAutoStartEntry)autoStart;
            var targetMode = serviceAutoStart.EnabledStartMode.Value.ToString();
            using (var m = new ManagementObject(string.Format("Win32_Service.Name=\"{0}\"", autoStart.Path))) {
                uint returnCode = (uint)m.InvokeMethod("ChangeStartMode", new object[] { targetMode });
                if (returnCode != 0) {
                    throw new Exception($"Failed to disable auto start (Return code: {returnCode})");
                }
            }
        }

        public void StartWatcher() {
            Logger.Trace("StartWatcher called");
            StopWatcher();
            MonitorCancellationTokenSource = new CancellationTokenSource();
            MonitorTask = System.Threading.Tasks.Task.Run(() => {
                MonitorChanges(MonitorCancellationTokenSource.Token);
            });
            Logger.Trace("Watcher started");
        }

        protected void MonitorChanges(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                var cancelled = token.WaitHandle.WaitOne(WatcherIntervalInMs);
                if (cancelled) {
                    break;
                }
                CheckChanges();
            }
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
            if (MonitorCancellationTokenSource == null) {
                Logger.Trace("No watcher running");
                return;
            }
            Logger.Trace("Stopping watcher");
            MonitorCancellationTokenSource.Cancel();
            MonitorTask.Wait();
            MonitorTask.Dispose();
            MonitorTask = null;
            MonitorCancellationTokenSource.Dispose();
            MonitorCancellationTokenSource = null;
            Logger.Trace("Stopped watcher");
        }

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
        public event AutoStartChangeHandler Enable;
        public event AutoStartChangeHandler Disable;
        #endregion
    }
}
