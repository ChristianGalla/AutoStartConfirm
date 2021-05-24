using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoStartConfirm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Tests {
    [TestClass()]
    public class AppTests {
        [TestMethod()]
        public void AppStarts() {
            //string[] arguments = new string[0];
            //var ret = App.Main(arguments);
            //Assert.AreEqual(0, ret);

            using (App app = new App()) {

                app.Start();
            }
        }
    }
}