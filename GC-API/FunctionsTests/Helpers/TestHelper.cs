using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Internal;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Models.Database.User;

namespace FunctionsTests.Helpers
{
    [TestClass]
    public class TestHelper
    {
        public static readonly List<(string name, string email, string password)> TestUsers = new List<(string, string, string)> {
            ("A Name", "e@mail.com", "password"),
            ("A Looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong name",
                "e@mail.com", "P@$$$$/W)\\0RD`^")
        };

        private static StreamWriter sw = null;
        private static StreamReader sr = null;
        private static ILoggerFactory lf = LoggerFactory.Create(builder =>
                builder.AddConsole()
#if DEBUG
                .SetMinimumLevel(LogLevel.Debug)
#else
                .SetMinimumLevel(LogLevel.Information)
#endif
        );
        
        [AssemblyInitialize]
        public static void Init(TestContext testContext)
        {
            ClearCosmosDb(testContext);
            AuthTestHelper.PrepareForJwtOperations(testContext);
        }

        public static void Register((string name, string email, string password) user)
        {
            if (DocumentDBRepository<GcUser>.Client is null) throw new ArgumentNullException(nameof(DocumentDBRepository<GcUser>.Client));
            var req = MakeRequest(new { user.name, user.email, user.password }, NullLogger.Instance);
            Functions.Register.Run(req, DocumentDBRepository<GcUser>.Client, NullLogger.Instance).GetAwaiter().GetResult();
        }

        /// <summary>
        /// This should be called if MakeRequest is called to dispose the internal streams
        /// </summary>
        internal static void Cleanup()
        {
            sw?.Dispose();
            sr?.Dispose();
        }

        public static HttpRequest MakeRequest(object toSerialize, ILogger logger = null)
        {
            string json = JsonConvert.SerializeObject(toSerialize);
            logger.LogInformation(json);
            return MakeRequest(json, logger);
        }

        public static HttpRequest MakeRequest(string body, ILogger logger = null)
        {
            HttpRequest request = new DefaultHttpRequest(new DefaultHttpContext()) { };
            request.Body = new MemoryStream();
            sw = new StreamWriter(request.Body);
            sw.Write(body);
            sw.Flush();
            request.Body.Seek(0, SeekOrigin.Begin);
            logger?.LogTrace("Length: " + request.Body.Length);
            return request;
        }

        public static ILogger MakeLogger([CallerMemberName] string name = "")
        {
            return lf.CreateLogger(name);
        }

        [TestMethod]
        public void TestMakeRequestString()
        {
            string val = "hello world";
            var req = MakeRequest(val);
            sr = new StreamReader(req.Body);
            string readVal = sr.ReadToEnd();
            Assert.AreEqual(val, readVal);
            Cleanup();
        }

        private class Temp { public string String1 { get; set; } public int Int1 { get; set; } }

        [TestMethod]
        public void TestMakeRequestClass()
        {
            var req = MakeRequest(new Temp());
            sr = new StreamReader(req.Body);
            var readVal = sr.ReadToEnd();
            Assert.IsInstanceOfType(readVal, typeof(Temp));
            Cleanup();
        }

        /// <summary>
        /// Delete any and ALL databases in Cosmos DB connection.
        /// </summary>
        private static async void ClearCosmosDb(TestContext testContext)
        {
            var endpoint = (string)testContext.Properties["endpoint"];
            var authKey = (string)testContext.Properties["authKey"];
            DocumentDBRepository<object>.Initialize(endpoint, authKey);
            foreach (var db in await DocumentDBRepository<object>.Client.ReadDatabaseFeedAsync())
            {
                await DocumentDBRepository<object>.Client.DeleteDatabaseAsync(db.SelfLink);
            }
        }

    }
}
