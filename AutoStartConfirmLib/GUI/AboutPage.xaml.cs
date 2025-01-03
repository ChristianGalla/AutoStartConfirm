// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using AutoStartConfirm.Update;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Reflection;
using Windows.ApplicationModel.Resources;

namespace AutoStartConfirm.GUI
{
    public sealed partial class AboutPage : Page, ISubPage, IDisposable
    {
        public string NavTitle { get; set; }

        private readonly IServiceScope ServiceScope = Ioc.Default.CreateScope();

        private readonly ResourceLoader ResourceLoader = new("AutoStartConfirmLib/Resources");

        private IUpdateService? updateService;

        public IUpdateService UpdateService
        {
            get
            {
                updateService ??= ServiceScope.ServiceProvider.GetRequiredService<IUpdateService>();
                return updateService;
            }
        }

        public string PublishVersion
        {
            get {
                if (UpdateService.IsStandalone)
                {
                    return string.Format(ResourceLoader.GetString("PublishVersion/Standalone"), Environment.Version.ToString());
                }
                else
                {
                    return string.Format(ResourceLoader.GetString("PublishVersion/FrameworkDependent"), Environment.Version.ToString());
                }
            }
        }


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

        private string? _license;

        public string? License
        {
            get
            {
                if (_license == null)
                {
                    string path = @"LICENSE";

                    if (File.Exists(path))
                    {
                        _license = File.ReadAllText(path);
                    }
                }
                return _license;
            }
        }

        private string? _thirdPartyLicenses;
        private bool disposedValue;

        public string? ThirdPartyLicenses
        {
            get
            {
                if (_thirdPartyLicenses == null)
                {
                    string path = @"Licenses\Licenses.txt";

                    if (File.Exists(path))
                    {
                        _thirdPartyLicenses = File.ReadAllText(path);
                    }
                }
                return _thirdPartyLicenses;
            }
        }

        public AboutPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            var resourceLoader = new ResourceLoader("AutoStartConfirmLib/Resources");
            NavTitle = resourceLoader.GetString("NavigationAbout/Content");
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceScope.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
