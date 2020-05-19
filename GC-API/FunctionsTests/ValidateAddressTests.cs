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
        private static readonly List<(string theater, string street, string city)> validAddresses = new List<(string, string, string)> {
            ("Toby's Dinner Theatre", "5900 Symphony Woods Rd", "Columbia"),
            ("Gershwin Theatre", "222 W 51st St", "New York")
        };
        private static readonly List<(string theater, string street, string city)> invalidAddresses = new List<(string, string, string)> {
            ("Toaby's Dinner Theatre", "5900 Symphony Woods Rd", "Columbia"),
            ("Gershwin Theatre", "222 W 51st Rd", "New York"),
            ("Gershwin Theatre", "222 W 51st St", "Nrw York")
        };

        [ClassInitialize]
        public static void ValidateAddressInitialize(TestContext _)
        {
            token = AuthTestHelper.GenerateValidJwt(TestHelper.MakeLogger());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHelper.Cleanup();
        }

        #region Positive

        [DataRow(0)]
        [DataRow(1)]
        [DataTestMethod]
        public void BasicQueryParameters(int validAddressIndex)
        {
            var logger = TestHelper.MakeLogger();
            var (theater, street, city) = validAddresses[validAddressIndex];
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Query = new QueryCollection
                (
                    new Dictionary<string, StringValues>() {
                        { "theater", theater },
                        { "street", street },
                        { "city",  city }
                    }
                ),
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            var result = Functions.ValidateAddress.Run(request, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var value = ((OkObjectResult) result).Value;
            Assert.AreEqual("True", value);
        }

        [DataRow(0)]
        [DataRow(1)]
        [DataTestMethod]
        public void BasicJsonBody(int validAddressIndex)
        {
            var logger = TestHelper.MakeLogger();
            var (theater, street, city) = validAddresses[validAddressIndex];
            var request = TestHelper.MakeRequest(new { theater, street, city }, logger);
            request.Headers.Add("Authorization", $"Bearer {token}");

            var result = Functions.ValidateAddress.Run(request, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var value = ((OkObjectResult) result).Value;
            Assert.AreEqual("True", value);
        }

        #endregion
        #region Negative

        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataTestMethod]
        public void TheaterDoesNotExist(int invalidAddressIndex)
        {
            var logger = TestHelper.MakeLogger();
            var (theater, street, city) = invalidAddresses[invalidAddressIndex];
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Query = new QueryCollection
                (
                    new Dictionary<string, StringValues>() {
                        { "theater", theater },
                        { "street", street },
                        { "city",  city }
                    }
                ),
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            var result = Functions.ValidateAddress.Run(request, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var value = ((OkObjectResult) result).Value;
            Assert.AreEqual("False", value);
        }

        [DataRow("theater")]
        [DataRow("street")]
        [DataRow("city")]
        [DataTestMethod]
        public void MissingParameter(string parameterToRemove)
        {
            var logger = NullLoggerFactory.Instance.CreateLogger(nameof(MissingParameter));
            var validAddressIndex = 0;
            var (theater, street, city) = validAddresses[validAddressIndex];
            var queryParams = new Dictionary<string, StringValues>()
            {
                { "theater", theater },
                { "street", street },
                { "city",  city }
            };
            queryParams.Remove(parameterToRemove);
            var request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Query = new QueryCollection(queryParams)
            };
            request.Headers.Add("Authorization", $"Bearer {token}");

            var result = Functions.ValidateAddress.Run(request, logger).GetAwaiter().GetResult();

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var value = ((BadRequestObjectResult) result).Value;
            Assert.IsInstanceOfType(value, typeof(string));
            string msg = ((string) value);
            Assert.IsTrue(msg.Contains("missing", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(msg.Contains(parameterToRemove));
        }

        #endregion
    }
}
