using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirmTests.TestHelpers
{
    public class STATestClassAttribute : TestClassAttribute
    {
        public override TestMethodAttribute GetTestMethodAttribute(TestMethodAttribute? testMethodAttribute)
        {
            if (testMethodAttribute is STATestMethodAttribute)
                return testMethodAttribute;

            return new STATestMethodAttribute(base.GetTestMethodAttribute(testMethodAttribute));
        }
    }
}
