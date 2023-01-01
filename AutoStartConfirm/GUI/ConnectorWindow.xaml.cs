using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace AutoStartConfirm.GUI
{
    /// <summary>
    /// Interaction logic for ConnectorWindow.xaml
    /// </summary>
    public partial class ConnectorWindow : Window, IDisposable
    {
        private bool disposedValue;

        protected ObservableCollection<ConnectorEnableRow> connectors;

        public readonly ISettingsService SettingsService;
        private readonly IAppStatus AppStatus;

        public ObservableCollection<ConnectorEnableRow> Connectors
        {
            get
            {
                if (connectors == null)
                {
                    connectors = new ObservableCollection<ConnectorEnableRow>();
                    connectors.CollectionChanged += CollectionChanged;
                    foreach (Category category in Enum.GetValues(typeof(Category)))
                    {
                        var row = new ConnectorEnableRow()
                        {
                            Category = category,
                            Enabled = !SettingsService.DisabledConnectors.Contains(category.ToString())
                        };
                        connectors.Add(row);
                    }
                }
                return connectors;
            }
        }

        public ConnectorWindow(ISettingsService settingsService, IAppStatus appStatus)
        {
            SettingsService = settingsService;
            AppStatus = appStatus;
            InitializeComponent();
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ConnectorEnableRow newItem in e.NewItems)
                {
                    newItem.PropertyChanged += PropertyChangedHandler;
                }
            }
        }

        private void PropertyChangedHandler(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    AppStatus.IncrementRunningActionCount();
                    if (e.PropertyName != "Enabled")
                    {
                        return;
                    }
                    var changedItem = (ConnectorEnableRow)sender;
                    var categoryString = changedItem.Category.ToString();
                    if (changedItem.Enabled)
                    {
                        while (SettingsService.DisabledConnectors.Contains(categoryString))
                        {
                            SettingsService.DisabledConnectors.Remove(categoryString);
                        }
                    }
                    else
                    {
                        SettingsService.DisabledConnectors.Add(categoryString);
                    }
                    SettingsService.Save();
                }
                finally
                {
                    AppStatus.DecrementRunningActionCount();
                }
            });
        }
        //protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        //{
        //    Hide();
        //    e.Cancel = true;
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (connectors != null)
                    {
                        connectors.CollectionChanged -= CollectionChanged;
                        foreach (var connetor in connectors)
                        {
                            connetor.PropertyChanged -= PropertyChangedHandler;
                        }
                    }
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
