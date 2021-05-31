using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace AutoStartConfirm.AutoStarts
{
    [Serializable]
    public abstract class AutoStartEntry : INotifyPropertyChanged {
        private Guid id;
        private string value;
        private string path;
        private Category category;
        private DateTime? date;
        private Change? change;
        private ConfirmStatus confirmStatus;
        private bool? isEnabled;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public Guid Id {
            get => id;
            set {
                id = value;
                NotifyPropertyChanged();
            }
        }

        public string Value {
            get => value;
            set {
                this.value = value;
                NotifyPropertyChanged();
            }
        }

        public string Path {
            get => path;
            set {
                path = value;
                NotifyPropertyChanged();
            }
        }

        public Category Category {
            get => category;
            set {
                category = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime? Date {
            get => date;
            set {
                date = value;
                NotifyPropertyChanged();
            }
        }

        public Change? Change {
            get => change;
            set {
                change = value;
                NotifyPropertyChanged();
            }
        }

        public ConfirmStatus ConfirmStatus {
            get => confirmStatus;
            set {
                confirmStatus = value;
                NotifyPropertyChanged();
            }
        }

        public bool? IsEnabled {
            get => isEnabled;
            set {
                isEnabled = value;
            }
        }

        [field: NonSerialized]
        public bool? CanBeEnabled { get; set; }

        [field: NonSerialized]
        public bool? CanBeDisabled { get; set; }

        [field: NonSerialized]
        public bool? CanBeAdded { get; set; }

        [field: NonSerialized]
        public bool? CanBeRemoved { get; set; }

        public AutoStartEntry() {
            Id = Guid.NewGuid();
            ConfirmStatus = ConfirmStatus.New;
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
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, this);
                ms.Position = 0;

                return (AutoStartEntry)formatter.Deserialize(ms);
            }
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
