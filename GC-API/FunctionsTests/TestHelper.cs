using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Internal;
using Xunit;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionsTests
{
    public class TestHelper
    {
        private static StreamWriter sw = null;
        private static StreamReader sr = null;
        private static ILoggerFactory lf = null;

        internal static void CleanUp()
        {
            sw?.Dispose();
            sr?.Dispose();
            lf?.Dispose();
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
            if (lf is null) lf = LoggerFactory.Create(builder =>
                builder.AddConsole()
#if DEBUG
                .SetMinimumLevel(LogLevel.Debug)
#else
                .SetMinimumLevel(LogLevel.Information)
#endif
                ) ;
            return lf.CreateLogger(name);
        }

        [Fact]
        public void TestMakeRequest()
        {
            string val = "hello world";
            var req = MakeRequest(val);
            sr = new StreamReader(req.Body);
            string readVal = sr.ReadToEnd();
            Assert.Equal(val, readVal);
            CleanUp();
        }

    }
}
