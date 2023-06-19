using AutoStartConfirm.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStartConfirm.GUI {
    public interface IMessageService {
        public enum AutoStartAction
        {
            Add,
            Remove,
            Enable,
            Disable,
            Ignore,
            RemoveIgnore,
        }

        public SemaphoreSlim DialogSemaphore {
            get;
        }

        public Task<bool> ShowConfirm(string caption, string message = "");

        public Task<bool> ShowConfirm(AutoStartEntry autoStart, AutoStartAction action);

        public Task<bool> ShowRemoveConfirm(IgnoredAutoStart autoStartn);

        public Task ShowError(string caption, string message = "");

        public Task ShowError(string caption, Exception error);

        public Task ShowSuccess(string caption, string message = "");

        public Task ShowSuccess(AutoStartEntry autoStart, AutoStartAction action);

        public Task ShowRemoveSuccess(IgnoredAutoStart autoStart);
    }
}