using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoStartConfirm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using AutoStartConfirm.AutoStarts;
using AutoStartConfirm.Notifications;

namespace AutoStartConfirm.Tests {
    [TestClass()]
    public class AppTests {
        private IAutoStartService AutoStartService = A.Fake<IAutoStartService>();
        private INotificationService NotificationServicee = A.Fake<INotificationService>();

        [TestMethod()]
        public void Starts() {
            using (App app = new App()) {
                AddFakes(app);

                app.Start(true);

                A.CallTo(() => AutoStartService.LoadCurrentAutoStarts()).MustHaveHappened();
                A.CallTo(() => AutoStartService.CurrentAutoStarts).MustHaveHappened();
                A.CallTo(() => AutoStartService.StartWatcher()).MustHaveHappened();
            }
        }

        private void AddFakes(App app) {
            app.AutoStartService = AutoStartService;
            app.NotificationService = NotificationServicee;
        }
    }
}