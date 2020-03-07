using Functions.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FunctionsTests.Helpers
{
    public static class AuthTestHelper
    {
        // Since apparently local.settings.json is only used for local (non-unit test) runs
        public static void PrepareForJwtOperations(TestContext testContext)
        {
            Environment.SetEnvironmentVariable("AuthenticationSecret", (string) testContext.Properties["AuthenticationSecret"]);
            Environment.SetEnvironmentVariable("SessionTokenDays", (string) testContext.Properties["SessionTokenDays"]);
        }

        public static string GenerateValidJwt(ILogger log, string email="e@mail.com")
        {
           return AuthenticationHelper.GenerateJwt(email, log);
        }
    }
}
