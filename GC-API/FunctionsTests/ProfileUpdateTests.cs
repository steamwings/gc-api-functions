using FunctionsTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models.Common.User;
using Models.Database.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsTests
{
    [TestClass]
    public class ProfileUpdateTests
    {
        public TestContext TestContext { get; set; }
        private static string token;
        private const int testUserIndex = 0;

        [ClassInitialize]
        public static void ProfileUpdateInitialize(TestContext _)
        {
            token = AuthTestHelper.GenerateValidJwt(TestHelper.MakeLogger(), TestHelper.TestUsers[0].email);
        }

        [TestInitialize]
        public void Initialize()
        {
            var endpoint = (string)TestContext.Properties["endpoint"];
            var authKey = (string)TestContext.Properties["authKey"];
            DocumentDBRepository<GcUser>.Initialize(endpoint, authKey, null, "/coreUser/email");
            TestHelper.Register(TestHelper.TestUsers[0]);
        }

        [TestMethod]
        public void BlankProfile()
        {
            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new UserProfile(), logger);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var result = Functions.ProfileUpdate.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(IStatusCodeActionResult));
            var statusCode = (result as IStatusCodeActionResult).StatusCode;
            Assert.AreEqual(200, statusCode);
        }
    }
}
