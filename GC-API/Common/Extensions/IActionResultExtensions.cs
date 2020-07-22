using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class IActionResultExtensions
    {
        public static Task<IActionResult> ToResult(this IActionResult result) => Task.FromResult(result);
    }
}
