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
using AutoStartConfirm.Business;

namespace AutoStartConfirm.Connectors.Tests
{
    [TestClass]
    public class AutoStartServiceTests: TestsBase
    {
        protected static readonly ILogger<AutoStartBusiness> LogService = A.Fake<ILogger<AutoStartBusiness>>();

        private AutoStartBusiness? Service;

        private readonly static string CurrentExePath = Environment.ProcessPath!;
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

            Service = new AutoStartBusiness(
                logger: LogService,
                connectorService: ConnectorService,
                settingsService: SettingsService,
                currentUserRun64Connector: CurrentUserRun64Connector,
                dispatchService: DispatchService,
                uacService: UacService,
                appStatus: AppStatus,
                messageService: MessageService
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
        public async Task ConfirmAdd_ConfirmsCurrentAutoStart() {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            await Service.ConfirmAdd(Guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, AutoStartEntry!.ConfirmStatus);
        }

        [TestMethod]
        public async Task ConfirmAdd_RaisesCurrentAutoStartEvents() {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
            var confirmEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.Confirm += confirmEventHandler;
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.CurrentAutoStartChange += changeEventHandler;

            await Service.ConfirmAdd(Guid);

            A.CallTo(() => confirmEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(confirmEventHandler).ToList()[0].Arguments[0]);
            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod]
        public async Task ConfirmAdd_ConfirmsHistoryAutoStart() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);

            await Service.ConfirmAdd(Guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, AutoStartEntry!.ConfirmStatus);
        }

        [TestMethod]
        public async Task ConfirmAdd_RaisesHistoryAutoStartEvents() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += changeEventHandler;

            await Service.ConfirmAdd(Guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod]
        public async Task ConfirmRemove_ConfirmsHistoryAutoStart() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);

            await Service.ConfirmRemove(Guid);

            Assert.AreEqual(ConfirmStatus.Confirmed, AutoStartEntry!.ConfirmStatus);
        }

        [TestMethod]
        public async Task ConfirmRemove_RaisesHistoryAutoStartEvents() {
            Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
            var changeEventHandler = A.Fake<AutoStartChangeHandler>();
            Service.HistoryAutoStartChange += changeEventHandler;

            await Service.ConfirmRemove(Guid);

            A.CallTo(() => changeEventHandler.Invoke(A<AutoStartEntry>._)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(AutoStartEntry, Fake.GetCalls(changeEventHandler).ToList()[0].Arguments[0]);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task RemoveAutoStart_RemovesAutoStart_SetStatusToReverted_AndRevertsDisabling(bool useGuid, bool canBeEnabled) {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry!)).Returns(canBeEnabled);
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Remove)).Returns(true);

            if (useGuid) {
                await Service.RemoveAutoStart(Guid);
            } else {
                await Service.RemoveAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Remove)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(AutoStartEntry!, IMessageService.AutoStartAction.Remove)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.RemoveAutoStart(AutoStartEntry!)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry!.ConfirmStatus);

            if (canBeEnabled) {
                A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            } else {
                A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustNotHaveHappened();
            }
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task RemoveAutoStart_DoesntShowDialogsIfFlagIsNotSet(bool useGuid, bool canBeEnabled)
        {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry!)).Returns(canBeEnabled);

            if (useGuid)
            {
                await Service.RemoveAutoStart(Guid, false);
            }
            else
            {
                await Service.RemoveAutoStart(AutoStartEntry!, false);
            }

            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.RemoveAutoStart(AutoStartEntry!)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry!.ConfirmStatus);

            if (canBeEnabled)
            {
                A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            }
            else
            {
                A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustNotHaveHappened();
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RemoveAutoStart_ShowsDialogOnError(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Remove)).Returns(true);
            A.CallTo(() => ConnectorService.RemoveAutoStart(AutoStartEntry!)).Throws(new Exception());

            if (useGuid)
            {
                Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
                await Service.RemoveAutoStart(Guid);
            }
            else
            {
                await Service!.RemoveAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RemoveAutoStart_ThrowsErrorIfFlagIsNotSet(bool useGuid)
        {
            A.CallTo(() => ConnectorService.RemoveAutoStart(AutoStartEntry!)).Throws(new Exception());

            try
            {
                if (useGuid)
                {
                    Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                    await Service.RemoveAutoStart(Guid, false);
                }
                else
                {
                    await Service!.RemoveAutoStart(AutoStartEntry!, false);
                }
                Assert.Fail("Expected exception");
            }
            catch (Exception)
            {
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task RemoveAutoStart_DoesNothingIfNotConfirmed(bool useGuid, bool canBeEnabled)
        {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry!)).Returns(canBeEnabled);
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Remove)).Returns(false);

            if (useGuid)
            {
                await Service.RemoveAutoStart(Guid);
            }
            else
            {
                await Service.RemoveAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Remove)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.RemoveAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.EnableAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DisableAutoStart_DisablesAutoStart_AndSetsStatusToDisabled(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Disable)).Returns(true);
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            if (useGuid) {
                await Service.DisableAutoStart(Guid);
            } else {
                await Service.DisableAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Disable)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(AutoStartEntry!, IMessageService.AutoStartAction.Disable)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.DisableAutoStart(AutoStartEntry!)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Disabled, AutoStartEntry!.ConfirmStatus);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DisableAutoStart_ShowsDialogOnError(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Disable)).Returns(true);
            A.CallTo(() => ConnectorService.DisableAutoStart(AutoStartEntry!)).Throws(new Exception());
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            if (useGuid)
            {
                await Service.DisableAutoStart(Guid);
            }
            else
            {
                await Service.DisableAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DisableAutoStart_ThrowsOnErrorIfFlagIsNotSet(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Disable)).Returns(true);
            A.CallTo(() => ConnectorService.DisableAutoStart(AutoStartEntry!)).Throws(new Exception());
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            try
            {
                if (useGuid)
                {
                    await Service.DisableAutoStart(Guid, false);
                }
                else
                {
                    await Service.DisableAutoStart(AutoStartEntry!, false);
                }
                Assert.Fail("Expected exception");
            } catch (Exception) {
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DisableAutoStart_DoesntShowDialogsIfFlagIsNotSet(bool useGuid)
        {
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            if (useGuid)
            {
                await Service.DisableAutoStart(Guid, false);
            }
            else
            {
                await Service.DisableAutoStart(AutoStartEntry!, false);
            }

            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.DisableAutoStart(AutoStartEntry!)).MustHaveHappenedOnceExactly();
            Assert.AreEqual(ConfirmStatus.Disabled, AutoStartEntry!.ConfirmStatus);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task DisableAutoStart_DoesNothingIfNotConfirmed(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Disable)).Returns(false);
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            if (useGuid)
            {
                await Service.DisableAutoStart(Guid);
            }
            else
            {
                await Service.DisableAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Disable)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.DisableAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AddAutoStart_AddsAndEnablesAutoStart(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Add)).Returns(true);

            if (useGuid) {
                Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                await Service.AddAutoStart(Guid);
            } else {
                await Service!.AddAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Add)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(AutoStartEntry!, IMessageService.AutoStartAction.Add)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => ConnectorService.AddAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AddAutoStart_ShowsDialogOnError(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Add)).Returns(true);
            A.CallTo(() => ConnectorService.AddAutoStart(AutoStartEntry!)).Throws(new Exception());

            if (useGuid)
            {
                Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                await Service.AddAutoStart(Guid);
            }
            else
            {
                await Service!.AddAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AddAutoStart_ThrowsErrorIfFlagIsNotSet(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Add)).Returns(true);
            A.CallTo(() => ConnectorService.AddAutoStart(AutoStartEntry!)).Throws(new Exception());

            try
            {
                if (useGuid)
                {
                    Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                    await Service.AddAutoStart(Guid, false);
                }
                else
                {
                    await Service!.AddAutoStart(AutoStartEntry!, false);
                }
                Assert.Fail("Expected exception");
            } catch (Exception)
            {
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AddAutoStart_DoesntShowDialogsIfFlagIsNotSet(bool useGuid)
        {
            if (useGuid)
            {
                Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                await Service.AddAutoStart(Guid, false);
            }
            else
            {
                await Service!.AddAutoStart(AutoStartEntry!, false);
            }

            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowSuccess(AutoStartEntry!, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => ConnectorService.AddAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AddAutoStart_DoesNothingIfNotConfirmed(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Add)).Returns(false);

            if (useGuid)
            {
                Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                await Service.AddAutoStart(Guid);
            }
            else
            {
                await Service!.AddAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Add)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.EnableAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.AddAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AddAutoStart_CatchesAlreadyExistExceptions(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Add)).Returns(true);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).Throws(new AlreadySetException());
            if (useGuid) {
                Service!.AllHistoryAutoStarts.Add(AutoStartEntry!);
                await Service.AddAutoStart(Guid);
            } else {
                await Service!.AddAutoStart(AutoStartEntry!);
            }

            Assert.AreEqual(ConfirmStatus.Reverted, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
            A.CallTo(() => ConnectorService.AddAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task EnableAutoStart_EnablesAutoStart(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Enable)).Returns(true);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).DoesNothing();
            if (useGuid) {
                Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
                await Service.EnableAutoStart(Guid);
            } else {
                await Service!.EnableAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Enable)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(AutoStartEntry!, IMessageService.AutoStartAction.Enable)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            Assert.AreEqual(ConfirmStatus.Enabled, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task EnableAutoStart_DoesntShowDialogsIfFlagIsNotSet(bool useGuid)
        {
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).DoesNothing();
            if (useGuid)
            {
                Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
                await Service.EnableAutoStart(Guid, false);
            }
            else
            {
                await Service!.EnableAutoStart(AutoStartEntry!, false);
            }

            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            Assert.AreEqual(ConfirmStatus.Enabled, AutoStartEntry!.ConfirmStatus);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task EnableAutoStart_ShowsDialogOnError(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Enable)).Returns(true);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).Throws(new Exception());
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            if (useGuid)
            {
                await Service.EnableAutoStart(Guid);
            }
            else
            {
                await Service.EnableAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task EnableAutoStart_ThrowsOnErrorIfFlagIsNotSet(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Enable)).Returns(true);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).Throws(new Exception());
            Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);

            try
            {
                if (useGuid)
                {
                    await Service.EnableAutoStart(Guid, false);
                }
                else
                {
                    await Service.EnableAutoStart(AutoStartEntry!, false);
                }
                Assert.Fail("Expected exception");
            }
            catch (Exception)
            {
            }

            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task EnableAutoStart_DoesNothingIfNotConfirmed(bool useGuid)
        {
            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Enable)).Returns(false);
            A.CallTo(() => ConnectorService.EnableAutoStart(AutoStartEntry!)).DoesNothing();
            if (useGuid)
            {
                Service!.AllCurrentAutoStarts.Add(AutoStartEntry!);
                await Service.EnableAutoStart(Guid);
            }
            else
            {
                await Service!.EnableAutoStart(AutoStartEntry!);
            }

            A.CallTo(() => MessageService.ShowConfirm(AutoStartEntry!, IMessageService.AutoStartAction.Enable)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.EnableAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeEnabled_ReturnsEnableStatus(bool canBeEnabled) {
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry!)).Returns(canBeEnabled);
            var ret = Service!.CanAutoStartBeEnabled(AutoStartEntry!);

            Assert.AreEqual(canBeEnabled, ret);
            A.CallTo(() => ConnectorService.CanBeEnabled(AutoStartEntry!)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeDisabled_ReturnsDisableStatus(bool canBeDisabled) {
            A.CallTo(() => ConnectorService.CanBeDisabled(AutoStartEntry!)).Returns(canBeDisabled);
            var ret = Service!.CanAutoStartBeDisabled(AutoStartEntry!);

            Assert.AreEqual(canBeDisabled, ret);
            A.CallTo(() => ConnectorService.CanBeDisabled(AutoStartEntry!)).MustHaveHappenedOnceExactly();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CanAutoStartBeAdded_ReturnsAddStatus(bool canBeAdded) {
            A.CallTo(() => ConnectorService.CanBeAdded(AutoStartEntry!)).Returns(canBeAdded);
            var ret = Service!.CanAutoStartBeAdded(AutoStartEntry!);

            Assert.AreEqual(canBeAdded, ret);
            A.CallTo(() => ConnectorService.CanBeAdded(AutoStartEntry!)).MustHaveHappenedOnceExactly();
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
        public async Task ToggleOwnAutoStart_AddsOwnAutoStart_If_NotSet()
        {
            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Add)).Returns(true);
            A.CallTo(() => ConnectorService.AddAutoStart(A<AutoStartEntry>.Ignored)).DoesNothing();
            await Service!.ToggleOwnAutoStart();
            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Add)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Add)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
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
        public async Task ToggleOwnAutoStart_DoesntShowDialogsIfFlagIsNotSeet()
        {
            A.CallTo(() => ConnectorService.AddAutoStart(A<AutoStartEntry>.Ignored)).DoesNothing();
            await Service!.ToggleOwnAutoStart(false);
            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
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
        public async Task ToggleOwnAutoStart_DoesNothingIfNotConfirmed()
        {
            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Add)).Returns(false);
            A.CallTo(() => ConnectorService.AddAutoStart(A<AutoStartEntry>.Ignored)).DoesNothing();
            await Service!.ToggleOwnAutoStart();
            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Add)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, A<IMessageService.AutoStartAction>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.AddAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.EnableAutoStart(A<AutoStartEntry>.Ignored)).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task ToggleOwnAutoStart_RemovesOwnAutoStart_If_Set()
        {
            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Remove)).Returns(true);
            var collection = new List<AutoStartEntry>() {
                OwnAutoStartEntry
            };
            A.CallTo(() => CurrentUserRun64Connector.GetCurrentAutoStarts()).Returns(collection);
            await Service!.ToggleOwnAutoStart();
            A.CallTo(() => MessageService.ShowConfirm(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Remove)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowSuccess(A<AutoStartEntry>.Ignored, IMessageService.AutoStartAction.Remove)).MustHaveHappenedOnceExactly();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<Exception>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => MessageService.ShowError(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => ConnectorService.RemoveAutoStart(A<AutoStartEntry>.Ignored)).WhenArgumentsMatch(
                (AutoStartEntry autoStart) =>
                    autoStart.Category == Category.CurrentUserRun64 &&
                    autoStart.Path == "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Auto Start Confirm" &&
                    autoStart.Value == CurrentExePath
            ).MustHaveHappened();
        }
    }
}