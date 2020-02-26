using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunctionsTests
{
    [TestClass]
    public class ValidateAddressTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestSuccessQuery1()
        {
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
            var logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");

            var response = Functions.ValidateAddress.Run(request, logger);
            response.Wait();

            // Check that the response is an "OK" response
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            //Assert.IsAssignableFrom<OkObjectResult>(response.Result);

            // Check that the contents of the response are the expected contents
            var v = ((OkObjectResult)response.Result).Value;
            Assert.AreEqual("True",v);
        }

        [TestMethod]
        public void TestSuccessJSON1()
        {
            var logger = TestHelper.MakeLogger();
            var body = new { theater = "Gershwin Theatre", street = "222 W 51st St", city = "New York" };
            var mapsRequest = TestHelper.MakeRequest(body, logger);

            var response = Functions.ValidateAddress.Run(mapsRequest, logger);
            response.Wait();

            // Check that the response is an "OK" response
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            //Assert.IsAssignableFrom<OkObjectResult>(response.Result);

            // Check that the contents of the response are the expected contents
            var v = ((OkObjectResult) response.Result).Value;

            Assert.AreEqual("True", v);
            TestHelper.CleanUp();
        }

        [TestMethod]
        public void TestFailureNoTheater()
        {
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
            var logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");

            var response = Functions.ValidateAddress.Run(request, logger);
            response.Wait();

            Assert.IsInstanceOfType(response.Result, typeof(BadRequestObjectResult));
            //Assert.IsAssignableFrom<BadRequestObjectResult>(response.Result);

            // Check that the contents of the response are the expected contents
            var result = (BadRequestObjectResult)response.Result;
            Assert.AreEqual("Theater, street, and city must be in query string or JSON request body.", result.Value);
        }
    }
}
