using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AutoStartConfirm.AutoStarts
{
    [Serializable]
    public abstract class AutoStartEntry
    {
        public Guid Id { get; set; }

        public string Value { get; set; }

        public string Path { get; set; }

        public Category Category { get; set; }

        public DateTime? AddDate { get; set; }

        public DateTime? RemoveDate { get; set; }

        public ConfirmStatus ConfirmStatus { get; set; }

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
    }
}
