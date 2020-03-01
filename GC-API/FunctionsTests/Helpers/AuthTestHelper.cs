using Functions.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;

namespace FunctionsTests.Helpers
{
    public static class AuthTestHelper
    {
        public static void PrepareForJwtOperations(TestContext testContext)
        {
            ConfigurationManager.AppSettings["AuthenticationSecret"] = (string) testContext.Properties["AuthenticationSecret"];
            ConfigurationManager.AppSettings["SessionTokenDays"] = (string) testContext.Properties["SessionTokenDays"];
        }

        public static string GenerateValidJwt(ILogger log, string email="e@mail.com")
        {
           return AuthenticationHelper.GenerateJwt(email, log);
        }
    }
}
