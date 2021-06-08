using System;

namespace AutoStartConfirm.GUI {
    public interface IMessageService {
        bool ShowConfirm(string caption, string message = "");
        void ShowError(string caption, string message = "");
        void ShowError(string caption, Exception error);
        void ShowSuccess(string caption, string message = "");
    }
}