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

        public MainWindow(
            ILogger<MainWindow> logger
        ) {
            Logger = logger;
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            Title = "Auto Start Confirm";
            Logger.LogTrace("Window opened");
        }

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
#pragma warning disable IDE0270 // Use coalesce expression
                if (item == null)
                {
                    item = sender.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => (string)x.Content == (string)args.InvokedItem);
                }
#pragma warning restore IDE0270 // Use coalesce expression
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
            foreach (NavigationViewItemBase item in MainNavigation.MenuItems.Cast<NavigationViewItemBase>())
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

        #endregion

        #region events

        public event AutoStartsActionHandler? ConfirmAdd;
        public event AutoStartsActionHandler? RevertAdd;
        public event AutoStartsActionIdHandler? RevertAddId;
        public event AutoStartsActionHandler? Enable;
        public event AutoStartsActionHandler? Disable;
        public event AutoStartsActionHandler? ConfirmRemove;
        public event AutoStartsActionHandler? RevertRemove;
        public event AutoStartsActionIdHandler? RevertRemoveId;
        public event EventHandler? ToggleOwnAutoStart;
        #endregion

    }
}
