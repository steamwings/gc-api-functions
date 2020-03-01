using Microsoft.VisualStudio.TestTools.UnitTesting;
using Functions.Authentication;
using FunctionsTests.Helpers;
using System.Configuration;

namespace FunctionsTests
{
    [TestClass]
    public class JwtTests
    {
        [TestMethod]
        public void GenerateValidate1()
        {
            var email = "e@mail.com";
            var logger = TestHelper.MakeLogger();
            var token = AuthenticationHelper.GenerateJwt(email, logger);
            Assert.IsTrue(AuthenticationHelper.ValidateJwt(token, email, logger));
        }
    }
}
