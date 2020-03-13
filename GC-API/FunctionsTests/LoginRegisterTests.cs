using FunctionsTests.Helpers;
using FunctionsTests.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models.User;

namespace FunctionsTests
{
    [TestClass]
    public class LoginRegisterTests
    {
        public TestContext TestContext { get; set; }
               
        [TestInitialize]
        public void Initialize()
        {
            var endpoint = (string) TestContext.Properties["endpoint"];
            var authKey = (string) TestContext.Properties["authKey"];
            DocumentDBRepository<GcUser>.Initialize(endpoint, authKey, null, "/coreUser/email");
        }

        [TestCleanup]
        public void Cleanup()
        {
            DocumentDBRepository<GcUser>.Teardown();
            TestHelper.Cleanup();
        }

        /// <summary>
        /// Test registration
        /// </summary>
        [DataRow("A Name", "e@mail.com", "password")]
        [DataRow("A Looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong name",
            "e@mail.com", "P@$$$$$W)0RD")]
        [DataTestMethod]
        public void Register1(string name, string email, string password)
        {
            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { name, email, password }, logger);

            var result = Functions.Register.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(CreatedResult));
            var value = ((CreatedResult) result).Value;
            var rName = value.GetPropertyValue<string>("name");
            var rToken = value.GetPropertyValue<string>("token");
            Assert.IsNotNull(rToken);
            Assert.IsNotNull(rName);
            Assert.AreEqual(name, rName);
        }

        /// <summary>
        /// Test that you can login after registering
        /// </summary>
        [DataRow("A Name", "e@mail.com", "password")]
        [DataRow("A Looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong name",
            "e@mail.com", "P@$$$$/W)\\0RD`^")]
        [DataTestMethod]
        public void RegisterLogin1(string name, string email, string password)
        {
            Register1(name, email, password);

            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { email, password }, logger);
            
            var result = Functions.Login.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var value = ((OkObjectResult) result).Value;
            var rName = value.GetPropertyValue<string>("name");
            var rToken = value.GetPropertyValue<string>("token");
            Assert.IsNotNull(rToken);
            Assert.IsNotNull(rName);
            Assert.AreEqual(name, rName);
        }

        /// <summary>
        /// Ensure we see a 409 conflict when registering the same person twice
        /// </summary>
        [DataRow("A Name", "e@mail.com", "password")]
        [DataRow("A Looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong name",
    "e@mail.com", "P@$$$$/W)\\0RD`^")]
        [DataTestMethod]
        public void RegisterConflict1(string name, string email, string password)
        {
            Register1(name, email, password);

            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { name, email, password }, logger);

            var result = Functions.Register.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(IStatusCodeActionResult));
            var code = ((StatusCodeResult) result).StatusCode;
            Assert.AreEqual(409, code); // Expect 409 Conflict
        }

    }
}
