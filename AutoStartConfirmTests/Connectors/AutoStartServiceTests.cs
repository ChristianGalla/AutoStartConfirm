using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using AutoStartConfirm.Models;
using AutoStartConfirm.Exceptions;
using AutoStartConfirm.Properties;
using AutoStartConfirm.Connectors.Registry;
using AutoStartConfirm.GUI;
using AutoStartConfirm.Helpers;
using AutoStartConfirm.Notifications;
using AutoStartConfirm.Update;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Windows.Services.Maps;
using Octokit;
using AutoStartConfirmTests;

namespace AutoStartConfirm.Connectors.Tests
{
    [TestClass]
    public class AutoStartServiceTests: TestsBase
    {
        protected static readonly ILogger<AutoStartService> LogService = A.Fake<ILogger<AutoStartService>>();

        private AutoStartService? Service;

        private readonly static string CurrentExePath = Environment.ProcessPath;
        private Guid Guid;
        private RegistryAutoStartEntry? AutoStartEntry;

        private readonly AutoStartEntry OwnAutoStartEntry = new RegistryAutoStartEntry() {
            Category = Category.CurrentUserRun64,
            Path = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm",
            Value = CurrentExePath
        };

        [TestInitialize]
        public void TestInitialize()
        {
            Guid = new Guid();
            AutoStartEntry = new RegistryAutoStartEntry()
            {
                Id = Guid,
                Category = Category.CurrentUserRun64,
                Path = "",
                Value = ""
            };

            // A.CallTo(() => ServiceProvider.GetService()).Returns();

            Service = new AutoStartService(
                logger: LogService,
                connectorService: ConnectorService,
                settingsService: SettingsService,
                currentUserRun64Connector: CurrentUserRun64Connector,
                dispatchService: DispatchService,
                uacService: UacService
            );
        }

        [TestCleanup]
        public new void TestCleanup()
        {
            base.TestCleanup();
            Fake.ClearRecordedCalls(LogService);
            Service?.Dispose();
            Service = null;
        }


        [TestMethod]
        public void TryGetCurrentAutoStart_ReturnsFalseIfNotFound() {
            var ret = Service!.TryGetCurrentAutoStart(Guid, out AutoStartEntry? retAutoStart);

            Assert.IsFalse(ret);
            Assert.IsNull(retAutoStart);
        }

        [TestMethod]
        public void TryGetCurrentAutoStart_ReturnsTrueAndAutoStartIfFound() {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            var ret = Service.TryGetCurrentAutoStart(Guid, out AutoStartEntry? retAutoStart);

            Assert.IsTrue(ret);
            Assert.AreEqual(AutoStartEntry, retAutoStart);
        }

        [TestMethod]
        public void TryGetHistoryAutoStart_ReturnsFalseIfNotFound() {
            var ret = Service!.TryGetHistoryAutoStart(Guid, out AutoStartEntry? retAutoStart);

            Assert.IsFalse(ret);
            Assert.IsNull(retAutoStart);
        }

        [TestMethod]
        public void TryGetHistoryAutoStart_ReturnsTrueAndAutoStartIfFound() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);

            var ret = Service.TryGetHistoryAutoStart(Guid, out AutoStartEntry? retAutoStart);

            Assert.IsTrue(ret);
            Assert.AreEqual(AutoStartEntry, retAutoStart);
        }

        [TestMethod]
        public void ConfirmAdd_ConfirmsCurrentAutoStart() {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            Service.ConfirmAdd(Guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, AutoStartEntry.ConfirmStatus);
        }

        [TestMethod]
        public void ConfirmAdd_RaisesCurrentAutoStartEvents() {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
            var confirmEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.Confirm += confirmEventHandler;
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.CurrentAutoStartChange += changeEventHandler;

            Service.ConfirmAdd(Guid);

            A.CallTo(() => confirmEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(confirmEventHandler).ToList()[0].Arguments[0]);
            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod]
        public void ConfirmAdd_ConfirmsHistoryAutoStart() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);

            Service.ConfirmAdd(Guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, AutoStartEntry.ConfirmStatus);
        }

        [TestMethod]
        public void ConfirmAdd_RaisesHistoryAutoStartEvents() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += changeEventHandler;

            Service.ConfirmAdd(Guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod]
        public void ConfirmRemove_ConfirmsHistoryAutoStart() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);

            Service.ConfirmRemove(Guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, AutoStartEntry.ConfirmStatus);
        }

        [TestMethod]
        public void ConfirmRemove_RaisesHistoryAutoStartEvents() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += changeEventHandler;

            Service.ConfirmRemove(Guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void RemoveAutoStart_RemovesAutoStart_SetStatusToReverted_AndRevertsDisabling(bool useGuid, bool canBeEnabled) {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry)).Returns(canBeEnabled);

            if (useGuid) {
                Service.RemoveAutoStart(Guid);
            } else {
                Service.RemoveAutoStart(AutoStartEntry);
            }

            A.CallTo(() => ConnectorService.RemoveAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry.ConfirmStatus);

            if (canBeEnabled) {
                A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            } else {
                A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustNotHaveHappened();
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DisableAutoStart_DisablesAutoStart_AndSetsStatusToDisabled(bool useGuid) {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            if (useGuid) {
                Service.DisableAutoStart(Guid);
            } else {
                Service.DisableAutoStart(AutoStartEntry);
            }

            A.CallTo(() => ConnectorService.DisableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Disabled, AutoStartEntry.ConfirmStatus);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AddAutoStart_AddsAndEnablesAutoStart(bool useGuid) {
            if (useGuid) {
                Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                Service.AddAutoStart(Guid);
            } else {
                Service!.AddAutoStart(AutoStartEntry!);
            }

            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => ConnectorService.AddAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AddAutoStart_CatchesAlreadyExistExceptions(bool useGuid) {
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).Throws(new AlreadySetException());
            if (useGuid) {
                Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                Service.AddAutoStart(Guid);
            } else {
                Service!.AddAutoStart(AutoStartEntry!);
            }

            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => ConnectorService.AddAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EnableAutoStart_EnablesAutoStart(bool useGuid) {
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).DoesNothing();
            if (useGuid) {
                Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
                Service.EnableAutoStart(Guid);
            } else {
                Service!.EnableAutoStart(AutoStartEntry!);
            }

            Assert.AreEqual(ConfirmStatus.Enabled, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeEnabled_ReturnsEnableStatus(bool canBeEnabled) {
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry!)).Returns(canBeEnabled);
            var ret = Service!.CanAutoStartBeEnabled(AutoStartEntry!);

            Assert.AreEqual(canBeEnabled, ret);
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeDisabled_ReturnsDisableStatus(bool canBeDisabled) {
            A.CallTo(() => ConnectorService.CanBeDisabled(AutoStartEntry!)).Returns(canBeDisabled);
            var ret = Service!.CanAutoStartBeDisabled(AutoStartEntry!);

            Assert.AreEqual(canBeDisabled, ret);
            A.CallTo(() => ConnectorService.CanBeDisabled(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeAdded_ReturnsAddStatus(bool canBeAdded) {
            A.CallTo(() => ConnectorService.CanBeAdded(AutoStartEntry!)).Returns(canBeAdded);
            var ret = Service!.CanAutoStartBeAdded(AutoStartEntry!);

            Assert.AreEqual(canBeAdded, ret);
            A.CallTo(() => ConnectorService.CanBeAdded(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeRemoved_ReturnsRemoveStatus(bool canBeRemoved) {
            A.CallTo(() => ConnectorService.CanBeRemoved(AutoStartEntry!)).Returns(canBeRemoved);
            var ret = Service!.CanAutoStartBeRemoved(AutoStartEntry!);

            Assert.AreEqual(canBeRemoved, ret);
            A.CallTo(() => ConnectorService.CanBeRemoved(AutoStartEntry!)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeAdded_UpdatesAutoStartEntryAndRaisesEvents(bool canBeAdded) {
            A.CallTo(() => ConnectorService.CanBeAdded(AutoStartEntry!)).Returns(canBeAdded);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service!.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += historyAutoStartChange;

            await Service.LoadCanBeAdded(AutoStartEntry!);

            Assert.AreEqual(canBeAdded, AutoStartEntry!.CanBeAdded!.Value);
            A.CallTo(() => changeEventHandler.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeRemoved_UpdatesAutoStartEntryAndRaisesEvents(bool canBeRemoved) {
            A.CallTo(() => ConnectorService.CanBeRemoved(AutoStartEntry!)).Returns(canBeRemoved);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service!.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += historyAutoStartChange;

            await Service.LoadCanBeRemoved(AutoStartEntry!);

            Assert.AreEqual(canBeRemoved, AutoStartEntry!.CanBeRemoved!.Value);
            A.CallTo(() => changeEventHandler.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeEnabled_UpdatesAutoStartEntryAndRaisesEvents(bool canBeEnabled) {
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry!)).Returns(canBeEnabled);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service!.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += historyAutoStartChange;

            await Service.LoadCanBeEnabled(AutoStartEntry!);

            Assert.AreEqual(canBeEnabled, AutoStartEntry!.CanBeEnabled!.Value);
            A.CallTo(() => changeEventHandler.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task LoadCanBeDisabled_UpdatesAutoStartEntryAndRaisesEvents(bool canBeDisabled) {
            A.CallTo(() => ConnectorService.CanBeDisabled(AutoStartEntry!)).Returns(canBeDisabled);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service!.CurrentAutoStartChange += changeEventHandler;
            var historyAutoStartChange = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += historyAutoStartChange;

            await Service.LoadCanBeDisabled(AutoStartEntry!);

            Assert.AreEqual(canBeDisabled, AutoStartEntry!.CanBeDisabled!.Value);
            A.CallTo(() => changeEventHandler.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => historyAutoStartChange.Invoke(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }


        [TestMethod]
        public void ToggleOwnAutoStart_AddsOwnAutoStart_If_NotSet() {
            A.CallTo(() => ConnectorService.AddAutoStart(A<AutoStartEntry>.Ignored)).DoesNothing();
            Service!.ToggleOwnAutoStart();
            A.CallTo(() => ConnectorService.AddAutoStart(A<AutoStartEntry>.Ignored)).WhenArgumentsMatch(
                (AutoStartEntry autoStart) =>
                    autoStart.Category == Category.CurrentUserRun64 &&
                    autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
                    autoStart.Value == CurrentExePath
            ).MustHaveHappened();
            A.CallTo(() => ConnectorService.EnableAutoStart(A<AutoStartEntry>.Ignored)).WhenArgumentsMatch(
                (AutoStartEntry autoStart) =>
                    autoStart.Category == Category.CurrentUserRun64 &&
                    autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
                    autoStart.Value == CurrentExePath
            ).MustHaveHappened();
        }

        [TestMethod]
        public void ToggleOwnAutoStart_RemovesOwnAutoStart_If_Set() {
            var collection = new List<AutoStartEntry>() {
                OwnAutoStartEntry
            };
            A.CallTo(() => CurrentUserRun64Connector.GetCurrentAutoStarts()).Returns(collection);
            Service!.ToggleOwnAutoStart();
            A.CallTo(() => ConnectorService.RemoveAutoStart(A<AutoStartEntry>.Ignored)).WhenArgumentsMatch(
                (AutoStartEntry autoStart) =>
                    autoStart.Category == Category.CurrentUserRun64 &&
                    autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
                    autoStart.Value == CurrentExePath
            ).MustHaveHappened();
        }
    }
}