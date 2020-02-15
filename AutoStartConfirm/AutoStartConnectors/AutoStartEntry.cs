using System;

namespace AutoStartConfirm.AutoStartConnectors
{
    [Serializable]
    public class AutoStartEntry
    {
        public string Name;

        public string Path;

        public Category Category;

        public DateTime DateTime = DateTime.Now;
    }
}
