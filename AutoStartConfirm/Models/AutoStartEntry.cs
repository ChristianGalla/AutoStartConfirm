using AutoStartConfirm.GUI;
using AutoStartConfirm.Helpers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AutoStartConfirm.Models
{
    [XmlInclude(typeof(FolderAutoStartEntry)), XmlInclude(typeof(RegistryAutoStartEntry)), XmlInclude(typeof(ScheduledTaskAutoStartEntry)), XmlInclude(typeof(ServiceAutoStartEntry))]
    [Serializable]
    public abstract class AutoStartEntry : INotifyPropertyChanged {
        private Guid id = Guid.NewGuid();
        private string value;
        private string path;
        private Category category;
        private DateTime? date;
        private Change? change;
        private ConfirmStatus confirmStatus = ConfirmStatus.New;
        private bool? isEnabled;

        [field: NonSerialized]
        private bool? canBeEnabled;

        [field: NonSerialized]
        private bool? canBeDisabled;

        [field: NonSerialized]
        private bool? canBeAdded;

        [field: NonSerialized]
        private bool? canBeRemoved;

        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid Id
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Value {
            get => value;
            set {
                if (this.value != value)
                {
                    this.value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Path {
            get => path;
            set {
                if (path != value)
                {
                    path = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Category Category {
            get => category;
            set {
                if (category != value)
                {
                    category = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DateTime? Date {
            get => date;
            set {
                if (date != value)
                {
                    date = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Change? Change {
            get => change;
            set {
                if (change != value)
                {
                    change = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ConfirmStatus ConfirmStatus {
            get => confirmStatus;
            set {
                if (confirmStatus != value)
                {
                    confirmStatus = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool? IsEnabled {
            get => isEnabled;
            set {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [field: NonSerialized]
        [JsonIgnore]
        internal readonly object LoaderLock = new();

        public bool? CanBeEnabled {
            get => canBeEnabled;
            set {
                if (canBeEnabled != value)
                {
                    canBeEnabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [field: NonSerialized]
        [JsonIgnore] // needed, because otherwise log serialization can hang
        public Task<bool>? CanBeEnabledLoader
        {
            get;
            set;
        }

        public bool? CanBeDisabled {
            get => canBeDisabled;
            set {
                if (canBeDisabled != value)
                {
                    canBeDisabled = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [field: NonSerialized]
        [JsonIgnore]
        public Task<bool>? CanBeDisabledLoader
        {
            get;
            set;
        }

        public bool? CanBeAdded {
            get => canBeAdded;
            set {
                if (canBeAdded != value)
                {
                    canBeAdded = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [field: NonSerialized]
        [JsonIgnore]
        public Task<bool>? CanBeAddedLoader
        {
            get;
            set;
        }


        public bool? CanBeRemoved {
            get => canBeRemoved;
            set {
                if (canBeRemoved != value)
                {
                    canBeRemoved = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [field: NonSerialized]
        [JsonIgnore]
        public Task<bool>? CanBeRemovedLoader
        {
            get;
            set;
        }

        public string CategoryAsString {
            get {
                return Category.ToString();
            }
        }

        public AutoStartEntry() {
        }


        public override bool Equals(Object obj) {
            // Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            } else {
                AutoStartEntry o = (AutoStartEntry)obj;
                return Id == o.Id ||
                    Category == o.Category && Value == o.Value && Path == o.Path;
            }
        }

        public override int GetHashCode() {
            return Category.GetHashCode() ^ Value.GetHashCode() ^ Path.GetHashCode();
        }

        public AutoStartEntry DeepCopy() {
            using (var ms = new MemoryStream()) {
                XmlSerializer serializer = new XmlSerializer(typeof(AutoStartEntry));
                serializer.Serialize(ms, this);
                ms.Position = 0;

                return (AutoStartEntry)serializer.Deserialize(ms);
            }
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // Directly calling invoke throws an exception if not called from main thread when binded to ui element
            using var ServiceScope = Ioc.Default.CreateScope();
            var dispatchService = ServiceScope.ServiceProvider.GetRequiredService<IDispatchService>();
            dispatchService.DispatcherQueue.TryEnqueue(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                // string.Empty calls are needed for bindings to the whole object
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            });
        }
    }
}
