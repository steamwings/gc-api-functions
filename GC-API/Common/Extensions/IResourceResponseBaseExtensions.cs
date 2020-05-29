using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Common.Extensions
{
    public static class IResourceResponseBaseExtensions
    {
        public static bool IsSuccessStatusCode(this IResourceResponseBase resp, ILogger log = null, [CallerMemberName] string callerName = "", [CallerLineNumber] int callerLine = -1, [CallerFilePath] string callerFile = "")
        {
            int statusCode = (int) resp.StatusCode;
            log?.LogTrace("Checking status code {Status} for {Caller} at {Line} in {File}", statusCode, callerName, callerLine, callerFile);
            return statusCode >= 200 && statusCode <= 299;
        }
    }
}
