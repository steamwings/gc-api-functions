using Microsoft.AspNetCore.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsTests.Extensions
{
    public static class IStatusCodeActionResultExtensions
    {
        public static bool IsSuccessStatusCode(this IStatusCodeActionResult result)
        {
            return result.StatusCode is int status && status >= 200 && status <= 299;
        }
    }
}
