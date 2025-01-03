using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;

namespace AutoStartConfirm.GUI
{

    public partial class NotifyIcon: ResourceDictionary
    {

        public NotifyIcon()
        {
            InitializeComponent();

            var showHideWindowCommand = (XamlUICommand)this["ShowHideWindowCommand"];
            showHideWindowCommand.ExecuteRequested += ToggleMainWindowHandler;

            var exitApplicationCommand = (XamlUICommand)this["ExitApplicationCommand"];
            exitApplicationCommand.ExecuteRequested += ExitHandler;
        }

        private void ExitHandler(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            Exit?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleMainWindowHandler(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            ToggleMainWindow?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Exit;

        public event EventHandler? ToggleMainWindow;
    }
}
