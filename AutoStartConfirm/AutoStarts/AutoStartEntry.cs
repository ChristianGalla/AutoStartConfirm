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
    }
}
