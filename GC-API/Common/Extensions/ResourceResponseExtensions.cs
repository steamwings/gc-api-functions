using Microsoft.Azure.Documents.Client;

namespace Common.Extensions
{
    public static class ResourceResponseExtensions
    {
        public static bool IsSuccessStatusCode(this ResourceResponseBase resp)
        {
            int statusCode = (int) resp.StatusCode;
            return statusCode >= 200 && statusCode <= 299;
        }
    }
}
