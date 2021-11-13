using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoStartConfirm.Models {
    public class ConnectorEnableRow : INotifyPropertyChanged {
        private Category category;

        public Category Category {
            get => category;
            set {
                category = value;
                NotifyPropertyChanged();
            }
        }

        public string CategoryName {
            get => category.ToString();
        }

        private bool enabled;

        public bool Enabled {
            get => enabled;
            set {
                enabled = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}