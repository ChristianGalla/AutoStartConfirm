using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoStartConfirm.GUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsClosed { get; private set; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IAutoStartService _autoStartService;

        public readonly IAppStatus _appStatus;

        private readonly ConnectorWindow _connectorWindow;

        private readonly AboutWindow _aboutWindow;

        private readonly IBusiness _business;

        public ObservableCollection<AutoStartEntry> CurrentAutoStarts
        {
            get
            {
                return _autoStartService.CurrentAutoStarts;
            }
        }

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts
        {
            get
            {
                return _autoStartService.HistoryAutoStarts;
            }
        }

        //public MainWindow(
        //    //IAutoStartService autoStartService,
        //    //IAppStatus appStatus,
        //    //ConnectorWindow connectorWindow,
        //    //AboutWindow aboutWindow,
        //    //IBusiness business
        //    )
        //{
        //    //_autoStartService = autoStartService;
        //    //_appStatus = appStatus;
        //    //_connectorWindow = connectorWindow;
        //    //_aboutWindow = aboutWindow;
        //    //_business = business;
        //    Logger.Trace("Window opened");
        //    InitializeComponent();
        //}
        protected override void OnClosed(EventArgs e)
        {
            Logger.Trace("Window closed");
            base.OnClosed(e);
            IsClosed = true;
        }

        #region Click handlers

        private void CurrentConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            _business.ConfirmAdd(autoStartEntry);
        }

        private void CurrentRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            _business.RevertAdd(autoStartEntry);
        }

        private void CurrentEnableButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            _business.Enable(autoStartEntry);
        }

        private void CurrentDisableButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            _business.Disable(autoStartEntry);
        }

        private void HistoryConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added)
            {
                _business.ConfirmAdd(autoStartEntry);
            }
            else if (autoStartEntry.Change == Change.Removed)
            {
                _business.ConfirmRemove(autoStartEntry);
            }
        }

        private void HistoryRevertButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added)
            {
                _business.RevertAdd(autoStartEntry.Id);
            }
            else if (autoStartEntry.Change == Change.Removed)
            {
                _business.RevertRemove(autoStartEntry.Id);
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }

        private void MenuItemAutoStart_Click(object sender, RoutedEventArgs e)
        {
            _business.ToggleOwnAutoStart();
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            _aboutWindow.Show();
        }

        private void MenuItemConnectors_Click(object sender, RoutedEventArgs e)
        {
            _connectorWindow.Show();
        }

        #endregion
    }
}
