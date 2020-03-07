using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Toolkit.Uwp.Notifications.NotificationActivator;

namespace AutoStartConfirm.DesktopNotifications {
    // The GUID CLSID must be unique to your app. Create a new GUID if copying this code.
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("DF651CDF-EC4A-4C98-9419-DE2709B00863"), ComVisible(true)]
    public class MyNotificationActivator : NotificationActivator {
        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId) {
            // TODO: Handle activation
        }
    }
}
