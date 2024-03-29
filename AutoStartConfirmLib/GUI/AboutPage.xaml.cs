// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace AutoStartConfirm.GUI
{
    public sealed partial class AboutPage : Page, ISubPage
    {
        public string NavTitle { get; set; }

#pragma warning disable CA1822 // Mark members as static
        public string Version
#pragma warning restore CA1822 // Mark members as static
        {
            get
            {
                return Assembly.GetEntryAssembly()!.GetName().Version!.ToString();
            }
        }

#pragma warning disable CA1822 // Mark members as static
        public string Copyright
#pragma warning restore CA1822 // Mark members as static
        {
            get
            {
                var attribute = (AssemblyCopyrightAttribute)Assembly.GetEntryAssembly()!.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))!;
                return attribute.Copyright;
            }
        }

        public AboutPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            var resourceLoader = new ResourceLoader("AutoStartConfirmLib/Resources");
            NavTitle = resourceLoader.GetString("NavigationAbout/Content");
        }
    }
}
