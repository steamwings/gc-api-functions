using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Models.User;
using Newtonsoft.Json;

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
        }

        [DataRow("A Name", "e@mail.com", "password")]
        [DataRow("A Looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong name",
            "e@mail.com", "P@$$$$$W)0RD")]
        [DataTestMethod]
        public void Register1(string name, string email, string password)
        {
            var logger = TestHelper.MakeLogger();
            var request = TestHelper.MakeRequest(new { name, email, password }, logger);
            var res = Functions.Register.Run(request, DocumentDBRepository<GcUser>.Client, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(res, typeof(OkObjectResult));
            var value = (string)((OkObjectResult)res).Value;
            var cUser = JsonConvert.DeserializeObject<CoreUser>(value);
            Assert.IsNotNull(cUser);
            Assert.AreEqual(email, cUser.email);
            Assert.AreEqual(name, cUser.name);
        }

        [TestMethod]
        public void RegisterLogin1()
        {
            Assert.IsTrue(true);
        }

    }
}
