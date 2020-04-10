using System;

namespace AutoStartConfirm.AutoStarts
{
    [Serializable]
    public class AutoStartEntry
    {
        public Guid Id { get; set; }

        public string Value { get; set; }

        public string Path { get; set; }

        public Category Category { get; set; }

        public DateTime? AddDate { get; set; }

        public DateTime? RemoveDate { get; set; }

        public ConfirmStatus ConfirmStatus { get; set; }

        public AutoStartEntry() {
            Id = Guid.NewGuid();
            ConfirmStatus = ConfirmStatus.None;
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
            return Category.GetHashCode() ^ Category.GetHashCode() ^ Value.GetHashCode() ^ Path.GetHashCode();
        }
    }
}
