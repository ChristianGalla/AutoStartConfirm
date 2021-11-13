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
        private App app;

        public App App {
            get {
                if (app == null) {
                    app = (App)Application.Current;
                }
                return app;
            }
            set {
                app = value;
            }
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            App.Close();
        }

        private void OwnAutoStartClicked(object sender, RoutedEventArgs e) {
            Application.Current.Dispatcher.Invoke(delegate {
                App.ToggleOwnAutoStart();
            });
        }
    }
}
