using Microsoft.VisualStudio.TestTools.UnitTesting;
using Functions.Authentication;
using FunctionsTests.Helpers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;

namespace FunctionsTests
{
    [TestClass]
    public class AuthenticationTests
    {
        [TestMethod]
        public void GenerateValidateBasic()
        {
            var logger = TestHelper.MakeLogger();
            IDictionary<string, object> d = new Dictionary<string, object>();
            var token = AuthenticationHelper.GenerateJwt(logger, d);
            Assert.AreEqual(AuthenticationHelper.JwtValidationResult.Valid, 
                AuthenticationHelper.ValidateJwt(logger, token, ref d));
        }

        [TestMethod]
        public void GenerateValidateEmailClaim()
        {
            var email = "e@mail.com";
            var logger = TestHelper.MakeLogger();
            var token = AuthenticationHelper.GenerateJwt(logger, email);
            IDictionary<string, object> d = new Dictionary<string, object> { { "email", email } };
            Assert.AreEqual(AuthenticationHelper.JwtValidationResult.Valid, 
                AuthenticationHelper.ValidateJwt(logger, token, ref d));
        }

        [TestMethod]
        public void GenerateAuthorizeBasic()
        {
            var logger = TestHelper.MakeLogger();
            IDictionary<string, object> d = new Dictionary<string, object>();
            var token = AuthenticationHelper.GenerateJwt(logger, d);
            HttpRequest request = new DefaultHttpRequest(new DefaultHttpContext()) { };
            request.Headers.Add("Authorization", $"Bearer {token}");
            bool result = AuthenticationHelper.Authorize(logger, request.Headers, out var resp);
            logger.LogDebug($"Status code: {(int) resp.StatusCode}");
            Assert.AreEqual(true, result);
        }
    }
}
