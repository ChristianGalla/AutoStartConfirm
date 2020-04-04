using System;

namespace AutoStartConfirm.AutoStarts
{
    [Serializable]
    public class AutoStartEntry
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public Category Category { get; set; }

        public DateTime DateTime { get; set; }

        public ConfirmStatus ConfirmStatus { get; set; }

        public AutoStartEntry() {
            DateTime = DateTime.Now;
            Id = Guid.NewGuid();
            ConfirmStatus = ConfirmStatus.None;
        }
    }
}
