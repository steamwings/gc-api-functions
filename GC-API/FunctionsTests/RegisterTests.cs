using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsTests
{
    [TestClass]
    public class RegisterTests
    {
        readonly string endpoint;
        public TestContext TestContext { get; set; }
               
        public RegisterTests()
        {
            var s = TestContext.Properties["endpoint"];
        }

        [TestMethod]
        public void TestRegister01()
        {

        }
    }
}
