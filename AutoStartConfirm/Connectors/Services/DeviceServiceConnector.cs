using AutoStartConfirm.Models;
using System.ServiceProcess;

namespace AutoStartConfirm.Connectors.Services
{
    public class DeviceServiceConnector : ServiceConnector, IDeviceServiceConnector
    {

        public override Category Category
        {
            get
            {
                return Category.DeviceService;
            }
        }

        protected override ServiceController[] GetServiceControllers()
        {
            return ServiceController.GetDevices();
        }
    }
}
