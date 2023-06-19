using AutoStartConfirm.GUI;
using AutoStartConfirm.Helpers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AutoStartConfirm.Models
{
    [Serializable]
    public class IgnoredAutoStart: INotifyPropertyChanged {
        private string value;
        private string path;
        private Category category;

        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        public required string Value {
            get => value;
            [MemberNotNull(nameof(value))]
            set {
                if (this.value != value)
                {
                    this.value = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public required string Path {
            get => path;
            [MemberNotNull(nameof(path))]
            set {
                if (path != value)
                {
                    path = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public required Category Category {
            get => category;
            set {
                if (category != value)
                {
                    category = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string CategoryAsString {
            get {
                return Category.ToString();
            }
        }

        public IgnoredAutoStart() {
        }

        [SetsRequiredMembers]
        public IgnoredAutoStart(AutoStartEntry autoStart)
        {
            Category = autoStart.Category;
            Path = autoStart.Path;
            Value = autoStart.Value;
        }

        public override bool Equals(object? obj) {
            // Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            } else {
                IgnoredAutoStart o = (IgnoredAutoStart)obj;
                return Category == o.Category && Value == o.Value && Path == o.Path;
            }
        }

        public override int GetHashCode() {
            return Category.GetHashCode() ^ Value.GetHashCode() ^ Path.GetHashCode();
        }

        public AutoStartEntry DeepCopy() {
            using var ms = new MemoryStream();
            XmlSerializer serializer = new(typeof(AutoStartEntry));
            serializer.Serialize(ms, this);
            ms.Position = 0;

            return (AutoStartEntry)serializer.Deserialize(ms)!;
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged == null)
            {
                return;
            }
            // Directly calling invoke throws an exception if not called from main thread when binded to ui element
            using var ServiceScope = Ioc.Default.CreateScope();
            var dispatchService = ServiceScope.ServiceProvider.GetRequiredService<IDispatchService>();
            dispatchService.TryEnqueue(() =>
            {
                try
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    // string.Empty calls are needed for bindings to the whole object
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                }
                catch {
                }
            });
        }
    }
}
