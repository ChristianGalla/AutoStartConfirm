using AutoStartConfirm.Models;
using System;
using System.Threading.Tasks;

namespace AutoStartConfirm
{
    public interface IApp
    {
        void ConfirmAdd(AutoStartEntry autoStart);
        void ConfirmAdd(Guid id);
        void ConfirmRemove(AutoStartEntry autoStart);
        void ConfirmRemove(Guid id);
        void Disable(AutoStartEntry autoStart);
        void Disable(Guid id);
        void Dispose();
        void Enable(AutoStartEntry autoStart);
        void Enable(Guid id);
        void InitializeComponent();
        void InstallUpdate(string msiUrl = null);
        void RevertAdd(AutoStartEntry autoStart);
        void RevertAdd(Guid id);
        void RevertRemove(AutoStartEntry autoStart);
        void RevertRemove(Guid id);
        void ShowAdd(Guid id);
        void ShowRemoved(Guid id);
        void Start(bool skipInitializing = false);
        void ToggleMainWindow();
        Task ToggleOwnAutoStart();
        void ViewUpdate();
    }
}