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
using Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using FunctionsTests.Extensions;

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
        {
            builder.AddDebug();
            builder.AddConsole()
#if DEBUG
                .SetMinimumLevel(LogLevel.Debug);
#else
                .SetMinimumLevel(LogLevel.Information);
#endif
        }
        );
        
        [AssemblyInitialize]
        public static void Init(TestContext testContext)
        {
            ClearCosmosDb(testContext);
            // Since local.settings.json is only used for local runs (and not unit tests)
            Environment.SetEnvironmentVariable("AuthenticationSecret", (string)testContext.Properties["AuthenticationSecret"]);
            Environment.SetEnvironmentVariable("SessionTokenDays", (string)testContext.Properties["SessionTokenDays"]);
        }

        /// <summary>
        /// Register a user for a unit test
        /// </summary>
        /// <returns>a JWT token string</returns>
        public static string Register((string name, string email, string password) user)
        {
            if (DocumentDBRepository<GcUser>.Client is null) throw new ArgumentNullException(nameof(DocumentDBRepository<GcUser>.Client));
            var req = MakeRequest(new { user.name, user.email, user.password }, NullLogger.Instance);
            var result = Functions.Primitives.Register.Run(req, DocumentDBRepository<GcUser>.Client, NullLogger.Instance).GetAwaiter().GetResult();
            return ((CreatedResult)result).Value.GetPropertyValue<string>("token");
        }

        /// <summary>
        /// This should be called if MakeRequest is called to dispose the internal streams
        /// </summary>
        internal static void Cleanup()
        {
            sw?.Dispose();
            sr?.Dispose();
        }

        public static HttpRequest EmptyRequest => new DefaultHttpRequest(new DefaultHttpContext()) { };

        public static HttpRequest MakeRequest(object toSerialize, ILogger logger = null)
        {
            string json = JsonConvert.SerializeObject(toSerialize);
            logger?.LogInformation(json);
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

        /// <summary>
        /// Create a unique log with the name of the caller
        /// </summary>
        /// <param name="name">Auto-populated</param>
        public static ILogger MakeLogger([CallerMemberName] string name = "")
        {
            return lf.CreateLogger(name);
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
