using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using FunctionsTests.Helpers;
using System.Collections.Generic;

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
        public void TestSuccessQuery()
        {
            var logger = TestHelper.MakeLogger();
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Query = new QueryCollection
                (
                    new Dictionary<string, StringValues>()
                    {
                        { "theater", "Toby's Dinner Theatre" },
                        { "street", "5900 Symphony Woods" },
                        { "city",  "Columbia" }
                    }
                ),
            };

            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = Functions.ValidateAddress.Run(request, logger);
            response.Wait();

            // Check that the response is "OK"
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));

            // Check that the contents of the response are the expected contents
            var value = ((OkObjectResult)response.Result).Value;
            Assert.AreEqual("True", value);
        }

        [TestMethod]
        public void TestSuccessJson()
        {
            var logger = TestHelper.MakeLogger();
            // Why did this stop working?
            // var body = new { theater = "Gershwin Theatre", street = "222 W 51st St", city = "New York" };
            var body = new { theater = "Toby's Dinner Theatre", street = "5900 Symphony Woods", city = "Columbia" };
            var mapsRequest = TestHelper.MakeRequest(body, logger);
            mapsRequest.Headers.Add("Authorization", $"Bearer {token}");

            var response = Functions.ValidateAddress.Run(mapsRequest, logger);
            response.Wait();

            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));

            var value = ((OkObjectResult) response.Result).Value;
            Assert.AreEqual("True", value);
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
                    new Dictionary<string, StringValues>()
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

            var result = (BadRequestObjectResult) response.Result;
            Assert.IsInstanceOfType(result.Value, typeof(string));
            
            string msg = ((string) result.Value);
            Assert.IsTrue(msg.Contains("missing", StringComparison.OrdinalIgnoreCase));
        }
    }
}
