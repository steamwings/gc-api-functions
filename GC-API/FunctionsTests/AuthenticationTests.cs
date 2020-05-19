using Microsoft.VisualStudio.TestTools.UnitTesting;
using Functions.Authentication;
using FunctionsTests.Helpers;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using System;
using FunctionsTests.Extensions;

namespace FunctionsTests
{
    [TestClass]
    public class AuthenticationTests
    {
        /// <summary>
        /// Basic JWT generation with empty claims and validation
        /// </summary>
        [TestMethod]
        public void GenerateValidateBasic_EmptyClaims()
        {
            var logger = TestHelper.MakeLogger();
            IDictionary<string, object> claims = new Dictionary<string, object>();
            var token = AuthenticationHelper.GenerateJwt(logger, claims); // Empty claims dictionary
            Assert.AreEqual(AuthenticationHelper.JwtValidationResult.Valid, 
                AuthenticationHelper.ValidateJwt(logger, token, ref claims));
        }

        /// <summary>
        /// Basic JWT generation with default claims and validation
        /// </summary>
        [TestMethod]
        public void GenerateValidateBasic_DefaultClaims()
        {
            var logger = TestHelper.MakeLogger();
            var token = AuthenticationHelper.GenerateJwt(logger); // Default claims dictionary
            IDictionary<string, object> claims = new Dictionary<string, object>();
            Assert.AreEqual(AuthenticationHelper.JwtValidationResult.Valid,
                AuthenticationHelper.ValidateJwt(logger, token, ref claims));
        }

        [TestMethod]
        public void GenerateValidateEmailClaim()
        {
            var email = "e@mail.com";
            var logger = TestHelper.MakeLogger();
            var token = AuthenticationHelper.GenerateJwt(logger, email);
            IDictionary<string, object> claimsToValidate = new Dictionary<string, object> { { "email", email } };
            Assert.AreEqual(AuthenticationHelper.JwtValidationResult.Valid, 
                AuthenticationHelper.ValidateJwt(logger, token, ref claimsToValidate));
        }

        [DataRow(1)]
        [DataRow(.1)]
        [DataRow(0)]
        [DataTestMethod]
        public void ValidateExpiredNegative(double daysExpired)
        {
            var logger = TestHelper.MakeLogger();
            var token = AuthenticationHelper.GenerateJwt(logger, expiration: DateTime.Now.AddDays(-daysExpired));
            IDictionary<string, object> claimsToValidate = new Dictionary<string, object>();
            Assert.AreEqual(AuthenticationHelper.JwtValidationResult.Expired,
                AuthenticationHelper.ValidateJwt(logger, token, ref claimsToValidate));
        }

        /// <summary>
        /// Basic test for <see cref="AuthenticationHelper.Authorize(ILogger, IHeaderDictionary, out Microsoft.AspNetCore.Mvc.ObjectResult)"/>
        /// </summary>
        /// <remarks>
        /// Note that the return value is intentionally NOT checked, since it should only be checked when the return value is <c>False</c>.
        /// </remarks>
        [TestMethod]
        public void BasicAuthorizationPositive()
        {
            var logger = TestHelper.MakeLogger();
            var token = AuthenticationHelper.GenerateJwt(logger);
            var request = new DefaultHttpRequest(new DefaultHttpContext()) { };
            request.Headers.Add("Authorization", $"Bearer {token}");
            bool result = AuthenticationHelper.Authorize(logger, request.Headers, out var resp);
            logger.LogDebug($"Status code: {resp.StatusCode}");
            Assert.IsTrue(result);
        }

        [DataRow(1)]
        [DataRow(.1)]
        [DataRow(0)]
        [DataTestMethod]
        public void AuthorizeExpiredNegative(double daysExpired)
        {
            var logger = TestHelper.MakeLogger();
            var token = AuthenticationHelper.GenerateJwt(logger, expiration: DateTime.Now.AddDays(-daysExpired));
            var request = new DefaultHttpRequest(new DefaultHttpContext()) { };
            request.Headers.Add("Authorization", $"Bearer {token}");
            bool result = AuthenticationHelper.Authorize(logger, request.Headers, out var resp);
            logger.LogDebug($"Status code: {resp.StatusCode}");
            Assert.IsFalse(result);
            Assert.IsFalse(resp.IsSuccessStatusCode());
        }
    }
}
