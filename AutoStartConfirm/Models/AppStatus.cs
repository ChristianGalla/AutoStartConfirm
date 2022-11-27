using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AutoStartConfirm.Models
{
    public class AppStatus : INotifyPropertyChanged {

        protected bool hasOwnAutoStart;

        public bool HasOwnAutoStart {
            get {
                return hasOwnAutoStart;
            }
            set {
                hasOwnAutoStart = value;
                NotifyPropertyChanged();
            }
        }

        protected int runningActionCount = 0;

        public int RunningActionCount {
            get {
                return runningActionCount;
            }
        }

        public void IncrementRunningActionCount() {
            Interlocked.Increment(ref runningActionCount);
            NotifyPropertyChanged("RunningActionCount");
        }

        public void DecrementRunningActionCount() {
            Interlocked.Decrement(ref runningActionCount);
            NotifyPropertyChanged("RunningActionCount");
        }


        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
