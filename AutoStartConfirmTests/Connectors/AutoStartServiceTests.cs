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
        Guid guid;
        RegistryAutoStartEntry autoStartEntry;

        [TestInitialize]
        public void TestInitialize() {
            service.Connectors = connectorService;
            guid = new Guid();
            autoStartEntry = new RegistryAutoStartEntry() {
                Id = guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };
        }

        [TestMethod()]
        public void TryGetCurrentAutoStart_ReturnsFalseIfNotFound() {
            var ret = service.TryGetCurrentAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsFalse(ret);
            Assert.IsNull(retAutoStart);
        }

        [TestMethod()]
        public void TryGetCurrentAutoStart_ReturnsTrueAndAutoStartIfFound() {
            service.CurrentAutoStarts.Add(autoStartEntry);

            var ret = service.TryGetCurrentAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsTrue(ret);
            Assert.AreEqual(autoStartEntry, retAutoStart);
        }

        [TestMethod()]
        public void TryGetHistoryAutoStart_ReturnsFalseIfNotFound() {
            var ret = service.TryGetHistoryAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsFalse(ret);
            Assert.IsNull(retAutoStart);
        }

        [TestMethod()]
        public void TryGetHistoryAutoStart_ReturnsTrueAndAutoStartIfFound() {
            service.HistoryAutoStarts.Add(autoStartEntry);

            var ret = service.TryGetHistoryAutoStart(guid, out AutoStartEntry retAutoStart);

            Assert.IsTrue(ret);
            Assert.AreEqual(autoStartEntry, retAutoStart);
        }

        [TestMethod()]
        public void ConfirmAdd_ConfirmsCurrentAutoStart() {
            service.CurrentAutoStarts.Add(autoStartEntry);

            service.ConfirmAdd(guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, autoStartEntry.ConfirmStatus);
        }

        [TestMethod()]
        public void ConfirmAdd_RaisesCurrentAutoStartEvents() {
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
            service.HistoryAutoStarts.Add(autoStartEntry);

            service.ConfirmAdd(guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, autoStartEntry.ConfirmStatus);
        }

        [TestMethod()]
        public void ConfirmAdd_RaisesHistoryAutoStartEvents() {
            service.HistoryAutoStarts.Add(autoStartEntry);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += changeEventHandler;

            service.ConfirmAdd(guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(autoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod()]
        public void ConfirmRemove_ConfirmsHistoryAutoStart() {
            service.HistoryAutoStarts.Add(autoStartEntry);

            service.ConfirmRemove(guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, autoStartEntry.ConfirmStatus);
        }

        [TestMethod()]
        public void ConfirmRemove_RaisesHistoryAutoStartEvents() {
            service.HistoryAutoStarts.Add(autoStartEntry);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += changeEventHandler;

            service.ConfirmRemove(guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(autoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void RemoveAutoStart_RemovesAutoStart_SetStatusToReverted_AndRevertsDisabling(bool useGuid, bool canBeEnabled) {
            service.CurrentAutoStarts.Add(autoStartEntry);
            A.CallTo(() => connectorService.CanBeEnabled(autoStartEntry)).Returns(canBeEnabled);

            if (useGuid) {
                service.RemoveAutoStart(guid);
            } else {
                service.RemoveAutoStart(autoStartEntry);
            }

            A.CallTo(() => connectorService.RemoveAutoStart(autoStartEntry)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Reverted, autoStartEntry.ConfirmStatus);

            if (canBeEnabled) {
                A.CallTo(() => connectorService.EnableAutoStart(autoStartEntry)).MustHaveHappenedOnceExactly();
            } else {
                A.CallTo(() => connectorService.EnableAutoStart(autoStartEntry)).MustNotHaveHappened();
            }
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DisableAutoStart_DisablesAutoStart_AndSetsStatusToDisabled(bool useGuid) {
            service.CurrentAutoStarts.Add(autoStartEntry);

            if (useGuid) {
                service.DisableAutoStart(guid);
            } else {
                service.DisableAutoStart(autoStartEntry);
            }

            A.CallTo(() => connectorService.DisableAutoStart(autoStartEntry)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Disabled, autoStartEntry.ConfirmStatus);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AddAutoStart_AddsAndEnablesAutoStart(bool useGuid) {
            if (useGuid) {
                service.HistoryAutoStarts.Add(autoStartEntry);
                service.AddAutoStart(guid);
            } else {
                service.AddAutoStart(autoStartEntry);
            }

            Assert.AreEqual(ConfirmStatus.Reverted, autoStartEntry.ConfirmStatus);
            A.CallTo(() => connectorService.EnableAutoStart(autoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => connectorService.AddAutoStart(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EnableAutoStart_EnablesAutoStart(bool useGuid) {
            if (useGuid) {
                service.CurrentAutoStarts.Add(autoStartEntry);
                service.EnableAutoStart(guid);
            } else {
                service.EnableAutoStart(autoStartEntry);
            }

            Assert.AreEqual(ConfirmStatus.Enabled, autoStartEntry.ConfirmStatus);
            A.CallTo(() => connectorService.EnableAutoStart(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeEnabled_ReturnsEnableStatus(bool canBeEnabled) {
            A.CallTo(() => connectorService.CanBeEnabled(autoStartEntry)).Returns(canBeEnabled);
            var ret = service.CanAutoStartBeEnabled(autoStartEntry);

            Assert.AreEqual(canBeEnabled, ret);
            A.CallTo(() => connectorService.CanBeEnabled(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeDisabled_ReturnsDisableStatus(bool canBeDisabled) {
            A.CallTo(() => connectorService.CanBeDisabled(autoStartEntry)).Returns(canBeDisabled);
            var ret = service.CanAutoStartBeDisabled(autoStartEntry);

            Assert.AreEqual(canBeDisabled, ret);
            A.CallTo(() => connectorService.CanBeDisabled(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeAdded_ReturnsAddStatus(bool canBeAdded) {
            A.CallTo(() => connectorService.CanBeAdded(autoStartEntry)).Returns(canBeAdded);
            var ret = service.CanAutoStartBeAdded(autoStartEntry);

            Assert.AreEqual(canBeAdded, ret);
            A.CallTo(() => connectorService.CanBeAdded(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeRemoved_ReturnsRemoveStatus(bool canBeRemoved) {
            A.CallTo(() => connectorService.CanBeRemoved(autoStartEntry)).Returns(canBeRemoved);
            var ret = service.CanAutoStartBeRemoved(autoStartEntry);

            Assert.AreEqual(canBeRemoved, ret);
            A.CallTo(() => connectorService.CanBeRemoved(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeAdded_UpdatesAutoStartEntryAndRaisesEvents(bool canBeAdded) {
            A.CallTo(() => connectorService.CanBeAdded(autoStartEntry)).Returns(canBeAdded);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += historyAutoStartChange;

            await service.LoadCanBeAdded(autoStartEntry);

            Assert.AreEqual(canBeAdded, autoStartEntry.CanBeAdded.Value);
            A.CallTo(() => changeEventHandler.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeRemoved_UpdatesAutoStartEntryAndRaisesEvents(bool canBeRemoved) {
            A.CallTo(() => connectorService.CanBeRemoved(autoStartEntry)).Returns(canBeRemoved);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += historyAutoStartChange;

            await service.LoadCanBeRemoved(autoStartEntry);

            Assert.AreEqual(canBeRemoved, autoStartEntry.CanBeRemoved.Value);
            A.CallTo(() => changeEventHandler.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeEnabled_UpdatesAutoStartEntryAndRaisesEvents(bool canBeEnabled) {
            A.CallTo(() => connectorService.CanBeEnabled(autoStartEntry)).Returns(canBeEnabled);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += historyAutoStartChange;

            await service.LoadCanBeEnabled(autoStartEntry);

            Assert.AreEqual(canBeEnabled, autoStartEntry.CanBeEnabled.Value);
            A.CallTo(() => changeEventHandler.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeDisabled_UpdatesAutoStartEntryAndRaisesEvents(bool canBeDisabled) {
            A.CallTo(() => connectorService.CanBeDisabled(autoStartEntry)).Returns(canBeDisabled);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            service.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            service.HistoryAutoStartChange += historyAutoStartChange;

            await service.LoadCanBeDisabled(autoStartEntry);

            Assert.AreEqual(canBeDisabled, autoStartEntry.CanBeDisabled.Value);
            A.CallTo(() => changeEventHandler.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(autoStartEntry)).MustHaveHappenedOnceExactly();
        }
    }
}