using Microsoft.AspNetCore.Http;

namespace Common.Extensions
{
    public static class IQueryCollectionExtensions
    {
        public static string GetQueryValue(this IQueryCollection collection, string name, string defaultValue = null)
        {
            return collection.TryGetValue(name, out var val)
                ? System.Net.WebUtility.UrlDecode(val)
                : defaultValue;
        }
    }
}
