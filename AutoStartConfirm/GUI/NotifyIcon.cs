using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AutoStartConfirm.GUI
{
    public partial class NotifyIcon {

        private readonly IBusiness _business;

        public NotifyIcon(IBusiness business)
        {
            _business = business;
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            App.Close();
        }

        private void OwnAutoStartClicked(object sender, RoutedEventArgs e) {
            Application.Current.Dispatcher.Invoke(delegate {
                _business.ToggleOwnAutoStart();
            });
        }
    }
}
