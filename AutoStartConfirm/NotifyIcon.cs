using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AutoStartConfirm
{
    public partial class NotifyIcon
    {
        public bool HasOwnAutoStart() {
            return App.GetInstance().HasOwnAutoStart;
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            App.Close();
        }

        private void OwnAutoStartClicked(object sender, RoutedEventArgs e) {
            Application.Current.Dispatcher.Invoke(delegate {
                App.GetInstance().ToggleOwnAutoStart();
                OwnAutoStartTaskbarMenuItem.IsChecked = HasOwnAutoStart();
            });
        }

        private void Opened(object sender, RoutedEventArgs e) {
            OwnAutoStartTaskbarMenuItem.IsChecked = HasOwnAutoStart();
        }
    }
}
