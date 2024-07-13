using AutoStartConfirm.Connectors;
using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using AutoStartConfirm.Business;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

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
            ILogger<MainWindow> logger,
            IAutoStartBusiness autoStartBusiness
        )
        {
            Logger = logger;
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            Activated += MainWindow_Activated;
            Title = "Auto Start Confirm";

            Logger.LogTrace("Window opened");
        }

        /// <summary>
        /// Fixes invisible top window buttons
        /// https://github.com/microsoft/microsoft-ui-xaml/issues/9540
        /// </summary>
        private void FixTitleIcons()
        {
            if (Application.Current.RequestedTheme == ApplicationTheme.Light)
            {
                AppWindow.TitleBar.ButtonForegroundColor = ColorHelper.FromArgb(255, 68, 68, 68);
                AppWindow.TitleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(255, 153, 153, 153);
            }
            else
            {
                AppWindow.TitleBar.ButtonForegroundColor = ColorHelper.FromArgb(255, 202, 202, 202);
                AppWindow.TitleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(255, 102, 102, 102);
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                AppTitleTextBlock.Foreground =
                    (SolidColorBrush)Application.Current.Resources["WindowCaptionForegroundDisabled"];
            }
            else
            {
                AppTitleTextBlock.Foreground =
                    (SolidColorBrush)Application.Current.Resources["WindowCaptionForeground"];
            }
            FixTitleIcons();
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
                if (item is NavigationViewItem navItem && item.Name == "HomeNavigation")
                {
                    MainNavigation.SelectedItem = item;
                    MainNavigation_Navigate(navItem);
                    break;
                }
            }
        }

        private void MainNavigation_Navigate(NavigationViewItem item)
        {
            switch (item.Name)
            {
                case "HomeNavigation":
                    ContentFrame.Navigate(typeof(MainPage));
                    break;

                case "AboutNavigation":
                    ContentFrame.Navigate(typeof(AboutPage));
                    break;
            }
        }

        #endregion

    }
}
