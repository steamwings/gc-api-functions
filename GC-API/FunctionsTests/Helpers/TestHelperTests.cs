using Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FunctionsTests.Helpers
{
    [TestClass]
    public class TestHelperTests
    {
        [TestMethod]
        public void TestMakeRequestString()
        {
            string val = "hello world";
            var req = TestHelper.MakeRequest(val);
            var sr = new StreamReader(req.Body);
            string readVal = sr.ReadToEnd();
            Assert.AreEqual(val, readVal);
            TestHelper.Cleanup();
        }

        private class Temp { public string String1 { get; set; } public int Int1 { get; set; } }

        [TestMethod]
        public void TestMakeRequestClass()
        {
            var req = TestHelper.MakeRequest(new Temp());
            var sr = new StreamReader(req.Body);
            Assert.IsTrue(sr.ReadToEnd().TryDeserialize<Temp>(out var readVal));
            Assert.IsInstanceOfType(readVal, typeof(Temp));
            TestHelper.Cleanup();
        }
    }
}
