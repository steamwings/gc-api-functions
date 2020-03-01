using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using FunctionsTests.Helpers;

namespace FunctionsTests
{
    [TestClass]
    public class ValidateAddressTests
    {
        public TestContext TestContext { get; set; }
        private static string token;

        [ClassInitialize]
        public static void ValidateAddressInitialize(TestContext tc)
        {
            token = AuthTestHelper.GenerateValidJwt(TestHelper.MakeLogger());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHelper.Cleanup();
        }

        [TestMethod]
        public void TestSuccessQuery1()
        {
            var logger = TestHelper.MakeLogger();
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Query = new QueryCollection
                (
                    new System.Collections.Generic.Dictionary<string, StringValues>()
                    {
                        { "theater", "Gershwin Theatre" },
                        { "street", "222 W 51st St" },
                        { "city",  "New York" }
                    }
                ),
            };

            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = Functions.ValidateAddress.Run(request, logger);
            response.Wait();

            // Check that the response is an "OK" response
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));

            // Check that the contents of the response are the expected contents
            var v = ((OkObjectResult)response.Result).Value;
            Assert.AreEqual("True",v);
        }

        [TestMethod]
        public void TestSuccessJson1()
        {
            var logger = TestHelper.MakeLogger();
            var body = new { theater = "Gershwin Theatre", street = "222 W 51st St", city = "New York" };
            var mapsRequest = TestHelper.MakeRequest(body, logger);
            mapsRequest.Headers.Add("Authorization", $"Bearer {token}");

            var response = Functions.ValidateAddress.Run(mapsRequest, logger);
            response.Wait();

            // Check that the response is an "OK" response
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));

            // Check that the contents of the response are the expected contents
            var v = ((OkObjectResult) response.Result).Value;
            Assert.AreEqual("True", v);
        }

        [TestMethod]
        public void TestFailureNoTheater()
        {
            // Use NullLogger for negative test
            var logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Query = new QueryCollection
                (
                    new System.Collections.Generic.Dictionary<string, StringValues>()
                    {
                        { "street", "222 W 51st St" },
                        { "city",  "New York" }
                    }
                )
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = Functions.ValidateAddress.Run(request, logger);
            response.Wait();

            Assert.IsInstanceOfType(response.Result, typeof(BadRequestObjectResult));

            // Check that the contents of the response are the expected contents
            var result = (BadRequestObjectResult)response.Result;
            Assert.IsInstanceOfType(result.Value, typeof(string));
            string msg = ((string)result.Value);
            Assert.IsTrue(msg.Contains("missing", StringComparison.OrdinalIgnoreCase));
        }
    }
}
