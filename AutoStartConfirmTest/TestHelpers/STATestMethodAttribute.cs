using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStartConfirmTest.TestHelpers
{
    public class STATestMethodAttribute(
        TestMethodAttribute? testMethodAttribute,
#pragma warning disable CS9113 // Parameter is unread.
        [CallerFilePath] string? declaringFilePath = "",
        [CallerLineNumber] int declaringLineNumber = 0
#pragma warning restore CS9113 // Parameter is unread.
        ) : TestMethodAttribute
    {
        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return await InvokeAsync(testMethod).ConfigureAwait(false);

            TestResult[]? result = null;
            var thread = new Thread(() => result = InvokeAsync(testMethod).GetAwaiter().GetResult());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return result!;
        }

        private async Task<TestResult[]> InvokeAsync(ITestMethod testMethod)
        {
            if (testMethodAttribute != null)
                return await testMethodAttribute.ExecuteAsync(testMethod).ConfigureAwait(false);

            var testResult = await testMethod.InvokeAsync(null).ConfigureAwait(false);
            return [testResult];
        }
    }
}
