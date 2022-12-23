using AutoStartConfirm.Models;
using System.ServiceProcess;

namespace AutoStartConfirm.Connectors.Services
{
    public class OtherServiceConnector : ServiceConnector, IOtherServiceConnector
    {

        public override Category Category
        {
            get
            {
                return Category.Service;
            }
        }

        protected override ServiceController[] GetServiceControllers()
        {
            return ServiceController.GetServices();
        }
    }
}
