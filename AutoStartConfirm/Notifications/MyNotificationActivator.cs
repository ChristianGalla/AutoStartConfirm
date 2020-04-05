using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Microsoft.Toolkit.Uwp.Notifications.NotificationActivator;

namespace AutoStartConfirm.Notifications {
    // The GUID CLSID must be unique to your app. Create a new GUID if copying this code.
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("DF651CDF-EC4A-4C98-9419-DE2709B00863"), ComVisible(true)]
    public class MyNotificationActivator : NotificationActivator {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId) {
            Application.Current.Dispatcher.Invoke(delegate
            {
                Logger.Trace("Called with {arguments}", invokedArgs);
                QueryString args = QueryString.Parse(invokedArgs);
                var app = App.GetInstance();
                switch (args["action"]) {
                    case "viewRemoved":
                        app.ShowRemoved(Guid.Parse(args["id"]));
                        break;
                    case "revertRemove":
                        app.RevertRemove(Guid.Parse(args["id"]));
                        break;
                    case "confirmRemove":
                        app.ConfirmRemove(Guid.Parse(args["id"]));
                        break;
                    case "viewAdd":
                        app.ShowAdd(Guid.Parse(args["id"]));
                        break;
                    case "revertAdd":
                        app.RevertAdd(Guid.Parse(args["id"]));
                        break;
                    case "confirmAdd":
                        app.ConfirmAdd(Guid.Parse(args["id"]));
                        break;
                    default:
                        break;
                }
            });
        }
    }
}
