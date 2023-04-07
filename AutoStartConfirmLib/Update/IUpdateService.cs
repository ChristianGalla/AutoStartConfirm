using System.Threading.Tasks;

namespace AutoStartConfirm.Update {
    public interface IUpdateService
    {
        Task CheckUpdateAndShowNotification();
    }

}