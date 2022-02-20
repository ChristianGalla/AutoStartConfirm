using AutoStartConfirm.Models;
using System;
using System.Threading.Tasks;

namespace AutoStartConfirm
{
    public interface IBusiness
    {
        void ConfirmAdd(AutoStartEntry autoStart);
        void ConfirmAdd(Guid id);
        void ConfirmRemove(AutoStartEntry autoStart);
        void ConfirmRemove(Guid id);
        void Disable(AutoStartEntry autoStart);
        void Disable(Guid id);
        void Enable(AutoStartEntry autoStart);
        void Enable(Guid id);
        void RevertAdd(AutoStartEntry autoStart);
        void RevertAdd(Guid id);
        void RevertRemove(AutoStartEntry autoStart);
        void RevertRemove(Guid id);
        void ShowAdd(Guid id);
        void ShowRemoved(Guid id);
        void Start(bool skipInitializing = false);
        Task ToggleOwnAutoStart();
        public void EnsureMainWindow(bool hidden = false);
        public void ToggleMainWindow();
    }
}