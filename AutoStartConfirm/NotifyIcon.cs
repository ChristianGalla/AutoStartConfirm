using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoStartConfirm
{
    public partial class NotifyIcon
    {
        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            App.Close();
        }
    }
}
