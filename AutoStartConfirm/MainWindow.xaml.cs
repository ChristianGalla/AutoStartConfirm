using AutoStartConfirm.AutoStarts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoStartConfirm
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsClosed { get; private set; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private App App {
            get {
                return App.GetInstance();
            }
        }

        public MainWindow()
        {
            Logger.Trace("Window opened");
            InitializeComponent();
            App.AutoStartService.Add += ChurrentAutoStartChangeHandler;
            App.AutoStartService.Remove += ChurrentAutoStartChangeHandler;
            CurrentAutoStartGrid.ItemsSource = App.AutoStartService.CurrentAutoStarts.Values;
        }

        protected override void OnClosed(EventArgs e)
        {
            Logger.Trace("Window closed");
            base.OnClosed(e);
            IsClosed = true;
        }


        #region Event handlers
        private void ChurrentAutoStartChangeHandler(AutoStartEntry addedAutostart) {
            CurrentAutoStartGrid.ItemsSource = App.AutoStartService.CurrentAutoStarts.Values;
            CurrentAutoStartGrid.Items.Refresh();
        }
        #endregion

    }
}
