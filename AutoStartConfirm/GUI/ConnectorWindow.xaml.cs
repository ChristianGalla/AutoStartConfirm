using AutoStartConfirm.Models;
using AutoStartConfirm.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutoStartConfirm.GUI {
    /// <summary>
    /// Interaction logic for ConnectorWindow.xaml
    /// </summary>
    public partial class ConnectorWindow : Window, IDisposable
    {

        protected ObservableCollection<ConnectorEnableRow> connectors;
        private bool disposedValue;


        private readonly ISettingsService _settingsService;
        private readonly IAppStatus _appStatus;

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
                            Enabled = !_settingsService.DisabledConnectors.Contains(category.ToString())
                        };
                        connectors.Add(row);
                    }
                }
                return connectors;
            }
        }

        public ConnectorWindow(
            ISettingsService settingsService,
            IAppStatus appStatus)
        {
            _settingsService = settingsService;
            _appStatus = appStatus;
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
                    _appStatus.IncrementRunningActionCount();
                    if (e.PropertyName != "Enabled")
                    {
                        return;
                    }
                    var changedItem = (ConnectorEnableRow)sender;
                    var categoryString = changedItem.Category.ToString();
                    if (changedItem.Enabled)
                    {
                        while (_settingsService.DisabledConnectors.Contains(categoryString))
                        {
                            _settingsService.DisabledConnectors.Remove(categoryString);
                        }
                    }
                    else
                    {
                        _settingsService.DisabledConnectors.Add(categoryString);
                    }
                    _settingsService.Save();
                }
                finally
                {
                    _appStatus.DecrementRunningActionCount();
                }
            });
        }

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
