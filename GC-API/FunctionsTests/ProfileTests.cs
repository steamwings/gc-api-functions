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
using Functions.Profile;
using Common.Extensions;
using FunctionsTests.Extensions;

namespace FunctionsTests
{
    [TestClass]
    public class ProfileTests
    {
        private static readonly List<UserProfile> testProfiles = new List<UserProfile>
        {
            new UserProfile(),
            new UserProfile { domains = "actor", bio = "amazing" },
        };
        public TestContext TestContext { get; set; }
        private string token;
        private const int testUserIndex = 0;

        [TestInitialize]
        public void Initialize()
        {
            var endpoint = (string)TestContext.Properties["endpoint"];
            var authKey = (string)TestContext.Properties["authKey"];
            DocumentDBRepository<GcUser>.Initialize(endpoint, authKey, null, "/coreUser/email");
            token = TestHelper.Register(TestHelper.TestUsers[testUserIndex]);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DocumentDBRepository<GcUser>.Teardown();
            TestHelper.Cleanup();
        }

        [TestMethod]
        public void GetBasic()
        {
            var logger = TestHelper.MakeLogger();
            var request = TestHelper.EmptyRequest;
            request.Headers.Add("Authorization", $"Bearer {token}");
            // Get the single registered user
            Assert.IsTrue(DocumentDBRepository<GcUser>.Client.FindUniqueItem(logger, x => x.CreateDocumentQuery<GcUser>("dbs/userdb/colls/usercoll"), out var user, out var response));

            var result = ProfileGet.Run(request, user.id, DocumentDBRepository<GcUser>.Client, logger);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var profile = (result as OkObjectResult).Value;
            Assert.IsInstanceOfType(profile, typeof(UserProfile));
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void UpdateBasic(int testProfileIndex)
        {
            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(testProfiles[testProfileIndex], logger);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var result = ProfileUpdate.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(IStatusCodeActionResult));
            var statusCode = (result as IStatusCodeActionResult).StatusCode;
            Assert.AreEqual(200, statusCode);
        }

        [TestMethod]
        public void UpdateConsecutive()
        {
            var logger = TestHelper.MakeLogger();

            for(int i = 0; i < testProfiles.Count; i++)
            {
                var request = TestHelper.MakeRequest(testProfiles[i], logger);
                request.Headers.Add("Authorization", $"Bearer {token}");

                var result = ProfileUpdate.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

                Assert.IsInstanceOfType(result, typeof(IStatusCodeActionResult));
                var statusCode = (result as IStatusCodeActionResult).StatusCode;
                Assert.AreEqual(200, statusCode);
            }
            for (int i = testProfiles.Count-2; i >= 0; i--)
            {
                var request = TestHelper.MakeRequest(testProfiles[i], logger);
                request.Headers.Add("Authorization", $"Bearer {token}");

                var result = ProfileUpdate.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

                Assert.IsInstanceOfType(result, typeof(IStatusCodeActionResult));
                var statusCode = (result as IStatusCodeActionResult).StatusCode;
                Assert.AreEqual(200, statusCode);
            }
        }

    }
}
