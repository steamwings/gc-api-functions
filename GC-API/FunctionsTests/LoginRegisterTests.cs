using Microsoft.AspNetCore.Mvc;
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
            DocumentDBRepository<GcUser>.Initialize(endpoint, authKey);
        }

        [TestCleanup]
        public void Cleanup()
        {
            DocumentDBRepository<GcUser>.Teardown();
            TestHelper.Cleanup();
        }

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
            var cUser = (CoreUser)((CreatedResult)result).Value;
            //var cUser = JsonConvert.DeserializeObject<CoreUser>(value);
            Assert.IsNotNull(cUser);
            Assert.AreEqual(email, cUser.email);
            Assert.AreEqual(name, cUser.name);
        }

        [DataRow("A Name", "e@mail.com", "password")]
        [DataRow("A Looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong name",
            "e@mail.com", "P@$$$$$W)0RD")]
        [DataTestMethod]
        public void RegisterLogin1(string name, string email, string password)
        {
            Register1(name, email, password);

            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { email, password }, logger);
            var result = Functions.Login.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var value = ((OkObjectResult) result).Value;
            Assert.IsInstanceOfType(value, typeof(CoreUser));
            var cUser = (CoreUser) value;
            Assert.AreEqual(email, cUser.email);
            Assert.AreEqual(name, cUser.name);
        }

    }
}
