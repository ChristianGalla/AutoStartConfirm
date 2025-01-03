namespace AutoStartConfirm.Models
{
    public class ConnectorEnableRow {
        private Category category;

        public Category Category {
            get => category;
            set {
                category = value;
            }
        }

        public string CategoryName {
            get => category.ToString();
        }

        public bool Enabled
        {
            get;
            set;
        }

    }
}