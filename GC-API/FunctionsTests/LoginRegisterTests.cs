using FunctionsTests.Helpers;
using FunctionsTests.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models.Database.User;

namespace FunctionsTests
{
    // TODO These existing tests can be broken out into LoginTests and RegisterTests
    // TODO After doing the above, we should add more register tests (multiple users) 
    // and more login tests (variations of multiple logins with multiple users)
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

        #region Register Positive

        /// <summary>
        /// Test registration
        /// </summary>
        [DataRow(0)]
        [DataRow(1)]
        [DataTestMethod]
        public void Register(int testUserIndex)
        {
            var (name, email, password) = TestHelper.TestUsers[testUserIndex];
            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { name, email, password }, logger);

            var result = Functions.Primitives.Register.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objResult = (ObjectResult)result;
            Assert.AreEqual(201, objResult.StatusCode);
            AssertValidLoginRegisterResponse(objResult.Value, name, email);
        }

        #endregion
        #region Register Negative
        
        /// <summary>
        /// Ensure we see a 409 conflict when registering the same person twice
        /// </summary>
        [DataRow(0)]
        [DataRow(1)]
        [DataTestMethod]
        public void RegisterConflict(int testUserIndex)
        {
            var (name, email, password) = TestHelper.TestUsers[testUserIndex];
            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { name, email, password }, logger);

            Register(testUserIndex);
            var result = Functions.Primitives.Register.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(IStatusCodeActionResult));
            var code = ((IStatusCodeActionResult) result).StatusCode;
            Assert.AreEqual(409, code); // Expect 409 Conflict
        }

        [DataRow("name")]
        [DataRow("email")]
        [DataRow("password")]
        [DataTestMethod]
        public void RegisterMissingParameter(string propertyToNull)
        {
            var logger = TestHelper.MakeLogger();
            var (name, email, password) = TestHelper.TestUsers[0];
            var requestBodyValues = new { name, email, password };
            requestBodyValues.SetAnonymousObjectProperty(propertyToNull, null);
            var request = TestHelper.MakeRequest(requestBodyValues, logger);

            var result = Functions.Primitives.Register.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var value = ((BadRequestObjectResult) result).Value;
            Assert.IsInstanceOfType(value, typeof(string));
            Assert.IsTrue(((string) value).Contains(propertyToNull));

        }

        #endregion
        #region RegisterLogin Positive

        /// <summary>
        /// Test that you can login after registering
        /// </summary>
        [DataRow(0)]
        [DataRow(1)]
        [DataTestMethod]
        public void RegisterLogin(int testUserIndex)
        {
            var (name, email, password) = TestHelper.TestUsers[testUserIndex];
            Register(testUserIndex);

            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { email, password }, logger);

            var result = Functions.Primitives.Login.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var value = ((OkObjectResult)result).Value;
            AssertValidLoginRegisterResponse(value, name, email);
        }

        #endregion
        #region RegisterLogin Negative

        [DataRow("email")]
        [DataRow("password")]
        [DataTestMethod]
        public void LoginMissingParameter(string propertyToNull)
        {
            var testUserIndex = 0;
            Register(testUserIndex);
            var logger = TestHelper.MakeLogger();
            var (name, email, password) = TestHelper.TestUsers[testUserIndex];
            var requestBodyValues = new { email, password };
            requestBodyValues.SetAnonymousObjectProperty(propertyToNull, null);
            var request = TestHelper.MakeRequest(requestBodyValues, logger);

            var result = Functions.Primitives.Login.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var value = ((BadRequestObjectResult)result).Value;
            Assert.IsInstanceOfType(value, typeof(string));
            Assert.IsTrue(((string) value).Contains(propertyToNull));
        }

        #endregion

        private void AssertValidLoginRegisterResponse(object value, string name, string email)
        {
            var rName = value.GetPropertyValue<string>("name");
            var rToken = value.GetPropertyValue<string>("token");
            var rEmail = value.GetPropertyValue<string>("email");
            var id = value.GetPropertyValue<string>("id");
            Assert.IsNotNull(rToken);
            Assert.IsNotNull(id);
            Assert.AreEqual(name, rName);
            Assert.AreEqual(email, rEmail);
        }
    }
}
