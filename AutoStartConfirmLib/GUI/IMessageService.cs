using AutoStartConfirm.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStartConfirm.GUI {
    public interface IMessageService {
        SemaphoreSlim DialogSemaphore {
            get;
        }

        Task<bool> ShowConfirm(string caption, string message = "");

        Task<bool> ShowConfirm(AutoStartEntry autoStart, string action);

        Task ShowError(string caption, string message = "");
        Task ShowError(string caption, Exception error);
        Task ShowSuccess(string caption, string message = "");
    }
}