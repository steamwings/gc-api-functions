using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Common.Extensions
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

        /// <summary>
        /// Creates an <see cref="ILogger"/> which always prints with a prefix generated from <paramref name="prefixes"/>
        /// </summary>
        /// <param name="log"></param>
        /// <param name="prefixes"></param>
        /// <returns>A new log</returns>
        public static ILogger GetLoggerWithPrefix(this ILogger log, params string[] prefixes)
        {
            return new LogWrapper(log, prefixes);
        }

        /// <summary>
        /// Return an ILogger that prefixes with the function name
        /// </summary>
        /// <remarks>TODO: This is sort of a hack, and should probably be replaced by a custom logger.</remarks>
        private class LogWrapper : ILogger
        {
            private readonly ILogger _wrapped;
            private readonly string _prefix;
            public LogWrapper(ILogger toWrap, params string[] prefixes)
            {
                _wrapped = toWrap;
                _prefix = prefixes.Aggregate((prefix, x) => prefix + "-" + x).Trim('-') + ": ";
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return _wrapped.BeginScope(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _wrapped.IsEnabled(logLevel);
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _wrapped.Log(logLevel, eventId, state, exception, 
                    (TState t, Exception e) => _prefix + formatter.Invoke(t, e));
            }

        }
    }
}
