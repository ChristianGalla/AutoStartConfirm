using AutoStartConfirm.Models;
using Microsoft.Extensions.Logging;
using System.ServiceProcess;

namespace AutoStartConfirm.Connectors.Services
{
    public class DeviceServiceConnector : ServiceConnector, IDeviceServiceConnector
    {
        public DeviceServiceConnector(ILogger<ServiceConnector> logger) : base(logger)
        {
        }

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
