using System.Threading.Tasks;

namespace AutoStartConfirm.Update {
    public interface IUpdateService
    {
        public bool IsStandalone { get; set; }

        public Task CheckUpdateAndShowNotification();
    }

}