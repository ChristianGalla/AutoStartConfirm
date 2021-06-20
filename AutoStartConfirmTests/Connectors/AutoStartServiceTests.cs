using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoStartConfirm.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using AutoStartConfirm.Models;

namespace AutoStartConfirm.Connectors.Tests {
    [TestClass()]
    public class AutoStartServiceTests {

        IAutoStartService service = new AutoStartService();
        IAutoStartConnectorService connectorService = A.Fake<IAutoStartConnectorService>();

        [TestInitialize]
        public void TestInitialize() {
            service.Connectors = connectorService;
        }

        [TestMethod()]
        public void TryGetCurrentAutoStart_ReturnsFalseIfNotFound() {
            var guid = new Guid();

            var ret = service.TryGetCurrentAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsFalse(ret);
            Assert.IsNull(retAutoStart);
        }

        [TestMethod()]
        public void TryGetCurrentAutoStart_ReturnsTrueAndAutoStartIfFound() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.CurrentAutoStarts.Add(autoStartEntry);

            var ret = service.TryGetCurrentAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsTrue(ret);
            Assert.AreEqual(autoStartEntry, retAutoStart);
        }

        [TestMethod()]
        public void TryGetHistoryAutoStart_ReturnsFalseIfNotFound() {
            var guid = new Guid();

            var ret = service.TryGetHistoryAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsFalse(ret);
            Assert.IsNull(retAutoStart);
        }

        [TestMethod()]
        public void TryGetHistoryAutoStart_ReturnsTrueAndAutoStartIfFound() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.HistoryAutoStarts.Add(autoStartEntry);

            var ret = service.TryGetHistoryAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsTrue(ret);
            Assert.AreEqual(autoStartEntry, retAutoStart);
        }

        [TestMethod()]
        public void ConfirmAdd_ConfirmsCurrentAutoStart() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.CurrentAutoStarts.Add(autoStartEntry);

            service.ConfirmAdd(guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, autoStartEntry.ConfirmStatus);
        }

        [TestMethod()]
        public void ConfirmAdd_RaisesCurrentAutoStartEvents() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.CurrentAutoStarts.Add(autoStartEntry);
            var confirmEventHandler = A.Fake<AutoStartChangeHandler>();
            service.Confirm += confirmEventHandler;
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.CurrentAutoStartChange += changeEventHandler;

            service.ConfirmAdd(guid);

            A.CallTo(() => confirmEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(autoStartEntry, Fake.GetCalls(confirmEventHandler).ToList()[0].Arguments[0]);
            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(autoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod()]
        public void ConfirmAdd_ConfirmsHistoryAutoStart() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.HistoryAutoStarts.Add(autoStartEntry);

            service.ConfirmAdd(guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, autoStartEntry.ConfirmStatus);
        }

        [TestMethod()]
        public void ConfirmAdd_RaisesHistoryAutoStartEvents() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.HistoryAutoStarts.Add(autoStartEntry);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += changeEventHandler;

            service.ConfirmAdd(guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(autoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod()]
        public void ConfirmRemove_ConfirmsHistoryAutoStart() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.HistoryAutoStarts.Add(autoStartEntry);

            service.ConfirmRemove(guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, autoStartEntry.ConfirmStatus);
        }

        [TestMethod()]
        public void ConfirmRemove_RaisesHistoryAutoStartEvents() {
            var guid = new Guid();
            var autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
            service.HistoryAutoStarts.Add(autoStartEntry);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += changeEventHandler;

            service.ConfirmRemove(guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(autoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }
    }
}