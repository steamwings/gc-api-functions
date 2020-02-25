using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsTests
{
    [TestClass]
    public class LoginRegisterTests
    {
        public TestContext TestContext { get; set; }
               
        [TestInitialize]
        public void Initialize()
        {
            string endpoint = TestContext.Properties["endpoint"].ToString();
            string authKey = TestContext.Properties["authKey"].ToString();
            DocumentDBRepository<GcUser>.Initialize(endpoint, authKey);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DocumentDBRepository<GcUser>.Teardown();
        }

        [TestMethod]
        public void Register1()
        {
            var logger = TestHelper.MakeLogger();
            var body = new { name = "A Name", email = "e@mail.com", password = "password" };
            var req = TestHelper.MakeRequest(body, logger);
            Functions.Register.Run()
        }

        [TestMethod]
        public void RegisterLogin1()
        {
            
        }

    }
}
