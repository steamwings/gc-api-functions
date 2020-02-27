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

namespace FunctionsTests
{
    [TestClass]
    public class TestHelper
    {
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
        public void TestMakeRequest()
        {
            string val = "hello world";
            var req = MakeRequest(val);
            sr = new StreamReader(req.Body);
            string readVal = sr.ReadToEnd();
            Assert.AreEqual(val, readVal);
            Cleanup();
        }

    }
}
