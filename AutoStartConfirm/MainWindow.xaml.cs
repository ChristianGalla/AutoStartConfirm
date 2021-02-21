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

        private bool HasOwnAutoStart {
            get {
                return App.HasOwnAutoStart;
            }
        }

        public MainWindow()
        {
            Logger.Trace("Window opened");
            InitializeComponent();
            App.AutoStartService.CurrentAutoStartChange += CurrentAutoStartChangeHandler;
            App.AutoStartService.AddAutoStartChange += AddAutoStartChangeHandler;
            App.AutoStartService.RemoveAutoStartChange += RemoveAutoStartChangeHandler;
            CurrentAutoStartGrid.ItemsSource = App.AutoStartService.CurrentAutoStarts.Values;
            AddedAutoStartGrid.ItemsSource = App.AutoStartService.AddedAutoStarts.Values;
            RemovedAutoStartGrid.ItemsSource = App.AutoStartService.RemovedAutoStarts.Values;
        }

        protected override void OnClosed(EventArgs e)
        {
            Logger.Trace("Window closed");
            base.OnClosed(e);
            IsClosed = true;
        }

        #region click handlers

        private void CurrentConfirmButton_Click(object sender, RoutedEventArgs e) {
            AddConfirmButton_Click(sender, e);
        }

        private void CurrentRemoveButton_Click(object sender, RoutedEventArgs e) {
            AddRevertButton_Click(sender, e);
        }

        private void AddConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.ConfirmAdd(autoStartEntry.Id);
        }

        private void AddRevertButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.RevertAdd(autoStartEntry.Id);
        }

        private void RemoveConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.ConfirmRemove(autoStartEntry.Id);
        }

        private void RemoveRevertButton_Click(object sender, RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            App.RevertRemove(autoStartEntry.Id);
        }

        #endregion


        #region Event handlers

        private void CurrentAutoStartChangeHandler(AutoStartEntry addedAutostart) {
            CurrentAutoStartGrid.ItemsSource = App.AutoStartService.CurrentAutoStarts.Values;
            CurrentAutoStartGrid.Items.Refresh();
        }

        private void AddAutoStartChangeHandler(AutoStartEntry addedAutostart) {
            AddedAutoStartGrid.ItemsSource = App.AutoStartService.AddedAutoStarts.Values;
            AddedAutoStartGrid.Items.Refresh();
        }

        private void RemoveAutoStartChangeHandler(AutoStartEntry addedAutostart) {
            RemovedAutoStartGrid.ItemsSource = App.AutoStartService.RemovedAutoStarts.Values;
            RemovedAutoStartGrid.Items.Refresh();
        }

        #endregion

        private void MenuItemExit_Click(object sender, RoutedEventArgs e) {
            window.Close();
        }

        private void MenuItemAutoStart_Click(object sender, RoutedEventArgs e) {
            App.GetInstance().ToggleOwnAutoStart();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e) {
            var aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }
    }
}
