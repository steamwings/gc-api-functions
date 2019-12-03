using System;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunctionsTests
{
    public class ValidateAddressTests
    {
        [Fact]
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
            Assert.IsAssignableFrom<OkObjectResult>(response.Result);

            // Check that the contents of the response are the expected contents
            var v = ((OkObjectResult)response.Result).Value;
            Assert.Equal("True",v);
            
        }

        [Fact]
        public void TestSuccessJSON1()
        {
            var logger = TestHelper.MakeLogger();

            string json = JsonConvert.SerializeObject(new { theater = "Gershwin Theatre", street = "222 W 51st St", city = "New York" });
            logger.LogInformation(json);

            var mapsRequest = TestHelper.MakeRequest(json, logger);
            var response = Functions.ValidateAddress.Run(mapsRequest, logger);
            response.Wait();

            // Check that the response is an "OK" response
            Assert.IsAssignableFrom<OkObjectResult>(response.Result);

            // Check that the contents of the response are the expected contents
            var v = ((OkObjectResult)response.Result).Value;

            Assert.Equal("True", v);
            TestHelper.CleanUp();
        }

        [Fact]
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

            Assert.IsAssignableFrom<BadRequestObjectResult>(response.Result);

            // Check that the contents of the response are the expected contents
            var result = (BadRequestObjectResult)response.Result;
            Assert.Equal("Theater, street, and city must be in query string or JSON request body.", result.Value);
        }
    }
}
