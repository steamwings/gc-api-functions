using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Functions.Extensions
{
    public static class ILoggerExtensions
    {
        public static bool NullWarning(this ILogger log, object checkValues, string message = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = -1)
        {
            return NullWarning(log, checkValues, out _, message, method, line);
        }

        /// <summary>
        /// Log a warning if any properties of <paramref name="checkValues"/> are null.
        /// </summary>
        /// <param name="log">this <see cref="ILogger"/></param>
        /// <param name="checkValues">object with properties to check</param>
        /// <param name="nulls">will output the name(s) of any null properties</param>
        /// <param name="message">can be included in warning log, optional</param>
        /// <param name="method">Auto-populated; do not use unless overloading.</param>
        /// <param name="line">Auto-populated; do not use unless overloading.</param>
        /// <returns><c>True</c> if there are any null properties.</returns>
        public static bool NullWarning(this ILogger log, object checkValues, out string nulls, string message = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = -1)
        {
            nulls = checkValues.GetType().GetProperties()
                .Aggregate("", (result, property) => property.GetValue(checkValues) is null ? result + property.Name + ',' : result).Trim(',');
            if(nulls != string.Empty)
            {
                log.LogWarning($"{method}: {message}: Value(s) {nulls} null at line {line}.");
                return true;
            }
            return false;
        }
    }
}
