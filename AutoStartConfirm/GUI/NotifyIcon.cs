using AutoStartConfirm.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Windows;

namespace AutoStartConfirm.GUI
{
    public partial class NotifyIcon : INotifyIcon
    {
        public NotifyIconDoubleClickCommand NotifyIconDoubleClickCommand = new();

        public NotifyIcon()
        {
            NotifyIconDoubleClickCommand.DoubleClick += IconDoubleClicked;
            InitializeComponent();
        }


        private void ExitClicked(object sender, EventArgs e)
        {
            Exit?.Invoke(sender, e);
        }

        private void OwnAutoStartClicked(object sender, EventArgs e)
        {
            OwnAutoStartToggle?.Invoke(sender, e);
        }

        private void IconDoubleClicked(object sender, EventArgs e)
        {
            Open?.Invoke(sender, e);
        }

        public event EventHandler Exit;
        public event EventHandler OwnAutoStartToggle;
        public event EventHandler Open;
    }
}
