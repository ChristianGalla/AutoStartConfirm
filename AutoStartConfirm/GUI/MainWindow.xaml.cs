using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace AutoStartConfirm.GUI
{
    public delegate void AutoStartsActionHandler(AutoStartEntry e);
    public delegate void AutoStartsActionIdHandler(Guid e);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        // public bool IsClosed { get; private set; }

        private readonly ILogger<MainWindow> Logger;

        private readonly ConnectorWindow ConnectorWindow;
        private readonly IAutoStartService AutoStartService;
        private readonly ISettingsService SettingsService;
        private readonly IAppStatus AppStatus;

        public ObservableCollection<AutoStartEntry> CurrentAutoStarts {
            get {
                return AutoStartService.CurrentAutoStarts;
            }
        }

        public ObservableCollection<AutoStartEntry> HistoryAutoStarts {
            get {
                return AutoStartService.HistoryAutoStarts;
            }
        }

        public MainWindow(
            ILogger<MainWindow> logger,
            ConnectorWindow connectorWindow,
            IAutoStartService autoStartService,
            IAppStatus appStatus,
            ISettingsService settingsService
        ) {
            Logger = logger;
            ConnectorWindow = connectorWindow;
            AutoStartService = autoStartService;
            SettingsService = settingsService;
            AppStatus = appStatus;
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            Title = "Auto Start Confirm";
            Logger.LogTrace("Window opened");
        }

        //protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        //{
        //    Hide();
        //    e.Cancel = true;
        //}

        //protected override void OnClosed(EventArgs e)
        //{
        //    Logger.LogTrace("Window closed");
        //    base.OnClosed(e);
        //    IsClosed = true;
        //}

        #region Click handlers
        private void MainNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                // find NavigationViewItem with Content that equals InvokedItem
                var item = sender.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Content == (string)args.InvokedItem);
                if (item == null)
                {
                    item = sender.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Content == (string)args.InvokedItem);
                }
                if (item == null)
                {
                    return;
                }
                MainNavigation_Navigate(item);
            }
        }

        private void MainNavigation_Loaded(object sender, RoutedEventArgs e)
        {
            // set the initial SelectedItem 
            foreach (NavigationViewItemBase item in MainNavigation.MenuItems)
            {
                if (item is NavigationViewItem navItem && item.Tag.ToString() == "Home")
                {
                    MainNavigation.SelectedItem = item;
                    MainNavigation_Navigate(navItem);
                    break;
                }
            }
        }

        private void MainNavigation_Navigate(NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "Home":
                    ContentFrame.Navigate(typeof(MainPage));
                    break;

                case "About":
                    ContentFrame.Navigate(typeof(AboutPage));
                    break;
            }
        }

        private void CurrentConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            ConfirmAdd?.Invoke(autoStartEntry);
        }

        private void CurrentRemoveButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            RevertAdd?.Invoke(autoStartEntry);
        }

        private void CurrentEnableButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            Enable?.Invoke(autoStartEntry);
        }

        private void CurrentDisableButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            Disable?.Invoke(autoStartEntry);
        }

        private void HistoryConfirmButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added) {
                ConfirmAdd?.Invoke(autoStartEntry);
            } else if (autoStartEntry.Change == Change.Removed) {
                ConfirmRemove?.Invoke(autoStartEntry);
            }
        }

        private void HistoryRevertButton_Click(object sender, RoutedEventArgs e) {
            var button = (Button)sender;
            var autoStartEntry = (AutoStartEntry)button.DataContext;
            if (autoStartEntry.Change == Change.Added) {
                RevertAddId?.Invoke(autoStartEntry.Id);
            } else if (autoStartEntry.Change == Change.Removed) {
                RevertRemoveId?.Invoke(autoStartEntry.Id);
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e) {
            // window.Hide();
        }

        private void MenuItemAutoStart_Click(object sender, RoutedEventArgs e) {
            ToggleOwnAutoStart?.Invoke(this, EventArgs.Empty);
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e) {
            // AboutWindow.Show();
        }

        private void MenuItemConnectors_Click(object sender, RoutedEventArgs e) {
            // ConnectorWindow.Show();
        }

        #endregion

        #region events

        public event AutoStartsActionHandler ConfirmAdd;
        public event AutoStartsActionHandler RevertAdd;
        public event AutoStartsActionIdHandler RevertAddId;
        public event AutoStartsActionHandler Enable;
        public event AutoStartsActionHandler Disable;
        public event AutoStartsActionHandler ConfirmRemove;
        public event AutoStartsActionHandler RevertRemove;
        public event AutoStartsActionIdHandler RevertRemoveId;
        public event EventHandler ToggleOwnAutoStart;
        #endregion

    }
}
