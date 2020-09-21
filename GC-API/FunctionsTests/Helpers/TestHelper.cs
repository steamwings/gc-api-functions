﻿using Microsoft.AspNetCore.Http;
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
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;

namespace FunctionsTests.Helpers
{
    /// <summary>
    /// Includes sample data and helper functions for unit and integration testing
    /// </summary>
    /// <remarks>
    /// This should probably be broken out into multiple helpers...?
    /// </remarks>
    [TestClass]
    public class TestHelper
    {
        private const string testDataFolder = @".\TestData";

        public static readonly List<(string name, string email, string password)> TestUsers = new List<(string, string, string)> {
            ("A Name", "e@mail.com", "password"),
            ("A Name That Is Looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong",
                "e@mail.com", "P@$$$$/W)\\0RD`^"),
            ("綠雞蛋和火腿", "綠雞蛋和@火腿.com", "🂫🂖🊂🺀🺇🤰🲙🅧🚢😬🛈👘🻪🙨🶞💼🧴🷗🲍🹸🛌🇐👦🡱🩢🷣🬳🈼🪛🡩🁏🅨🔹🇮🂕🞑🎦🐥😌🖣🷚🠃🰳🨬🣄🠾🀶🺈🁎🗠🔴🱽🶇🛎🃐🰫🪺🞜🟲🠻🪏🹨🐻🬴🰴🨇🺮🃊🡲🤂🏸🖃🤩🴲🎬🠉🟪🲆🳌🆓🕵🉌🨈🆏🨬🰀🗙🕉🯸🊠🐢🰯🝌🃼🞋🋰🋛🬨🹕📠🩹🊉🛘🛺🊥🚜🞴💉🜍😍🣆🏥🷊🍴🅵🵊🯊💆🶇🢠🭣🅵😌💲🫂📽🟈🍍🩳💼🴍🨑🋧🉎🯤🂡🗁🥓🞵🀿🨎🜨🻫🕜🁻🃤🻌🦔🍫🺏🨚🉜🤗🹋📷🳞🱰🩏💤🛵🙮🞕🞓🢫🻟🵷🜑😺🬞🢽🊕🺝🇚🷹🔃🻹🇼🚀🛲🟥🩽🆏🤛🟌🟁🷉🖸🲋😢🅤🈕🲦🀶🏔🁬🰈🏉🇦🅹🅰🶷🞣🤕🳻🩘🩾🴜🩒🝤🬈🖫💇🢃📾🰉🆁🬈🄒🠨🪬😌🆷🵶🖼🔓📟🜬🺻🩽🹘🲁🋘🂤🀏🺱🱅📉🈎🛦🴬🣿🮔🆤🦰🄵🭕🮅🪸🮭🇀👞🝧🜉🫑🹤🆉🕎🠍🰲🛋🰲🂀🗃🴆🞡🊔🊍🞴🍫🣠🤆🖫🇄🰻🸍👋🄱🵝🱾🟠🭴🡅🦟🤶🻦🱪🡇🐽😶👳🪩🕾🗑🛥🟘🞽🝖🊢🊖🭥"),
        };

        public static readonly List<string> TestPictures = new List<string>();

        public enum StorageContainer
        {
            ProfilePics
        }

        private static readonly Dictionary<string, string> TestPictureUrlsToPaths = new Dictionary<string, string>
        {
            { "https://via.placeholder.com/600/b0f7cc", "pic1" }
        };

        private static StreamWriter sw = null;
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
        public static async Task Init(TestContext testContext)
        {
            ClearCosmosDb(testContext);

            // local.settings.json is only used for local runs (and not unit/integration tests which just get *.runsettings from TestContext)
            // This allows configuration values from TestContext (from a *.runsettings file) to be read the same way in unit tests as for local runs and production
            foreach (DictionaryEntry property in testContext.Properties)
            {
                if (property.Value is string value)
                {
                    Environment.SetEnvironmentVariable((string)property.Key, value);
                }
            }

            if (Directory.Exists(testDataFolder))
                Directory.Delete(testDataFolder, true);
            Directory.CreateDirectory(testDataFolder);

            // Download and create test files
            using var http = new HttpClient();
            foreach(var entry in TestPictureUrlsToPaths)
            {
                using var fileStream = File.OpenWrite(entry.Value); 
                using var downloadStream = await http.GetStreamAsync(entry.Key);
                downloadStream.CopyTo(fileStream);
                TestPictures.Add(entry.Value);
            }
        }

        public static void SetupUserDb(TestContext context)
        {
            var endpoint = (string)context.Properties["endpoint"];
            var authKey = (string)context.Properties["authKey"];
            DocumentDBRepository<GcUser>.Initialize(endpoint, authKey, null, "/coreUser/email");
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
            return ((ObjectResult)result).Value.GetPropertyValue<string>("token");
        }

        /// <summary>
        /// Assert there is only one registered user and retrieve it
        /// </summary>
        /// <param name="log"></param>
        /// <returns>the only registered <see cref="GcUser"/></returns>
        public static GcUser GetOnlyUser(ILogger log)
        {
            Assert.IsTrue(DocumentDBRepository<GcUser>.Client.TryFindUniqueItem(log, x => x.CreateDocumentQuery<GcUser>("dbs/userdb/colls/usercoll"),
                out var user, out _));
            return user;
        }

        /// <summary>
        /// Get a user by id and assert there is only one with that id
        /// </summary>
        /// <param name="log"></param>
        /// <param name="id">user id to search for</param>
        /// <returns>the <see cref="GcUser"/> with <paramref name="id"/></returns>
        public static GcUser GetUser(ILogger log, string id)
        {
            Assert.IsTrue(DocumentDBRepository<GcUser>.Client.TryFindUniqueItem(log, x => x.CreateDocumentQuery<GcUser>("dbs/userdb/colls/usercoll")
                .Where(x => x.id == id), out var user, out _));
            return user;
        }

        /// <summary>
        /// This should be called if MakeRequest is called to dispose the internal streams
        /// </summary>
        internal static void Cleanup()
        {
            DocumentDBRepository<GcUser>.Teardown(); // This is safe to call even when Client is null
            sw?.Dispose();
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
            logger?.LogTrace("Constructed request has length: " + request.Body.Length);
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

        public static CloudBlobContainer CreateStorageContainer(TestContext testContext, StorageContainer container)
        {
            var storageBlobEndpoint = (string)testContext.Properties["StorageBlobEndpoint"];
            var storageAccountName = (string)testContext.Properties["SharedStorageAccountName"];
            var storageKey = (string)testContext.Properties["SharedStorageKey"];

            var credentials = new StorageCredentials(storageAccountName, storageKey);
            var uri = new Uri(storageBlobEndpoint + '/' + storageAccountName + '/' + ContainerName(container) + '/');
            var blobContainer = new CloudBlobContainer(uri, credentials);

            blobContainer.CreateIfNotExists(BlobContainerPublicAccessType.Container);

            return blobContainer;
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

        /// <summary>
        /// Get the actual name of the container
        /// </summary>
        /// <param name="container"></param>
        /// <returns>the name string</returns>
        /// <remarks>
        /// If simpler, this could be made programmatic by adding dashes and making lowercase.
        /// This could be made an extension method if moved to a static class.
        /// </remarks>
        private static string ContainerName(StorageContainer container)
            => container switch
            {
                StorageContainer.ProfilePics => "profile-pics",
                _ => throw new NotImplementedException(),
            };
    }
}
