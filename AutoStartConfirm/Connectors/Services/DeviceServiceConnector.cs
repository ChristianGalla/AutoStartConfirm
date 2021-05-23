using AutoStartConfirm.AutoStarts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Connectors {
    class DeviceServiceConnector : ServiceConnector {

        public override Category Category {
            get {
                return Category.DeviceService;
            }
        }

        protected override ServiceController[] GetServiceControllers() {
            return ServiceController.GetDevices();
        }
    }
}
