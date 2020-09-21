using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Common.Extensions
{
    public static class ILoggerExtensions
    {
        /// <inheritdoc cref="CheckNull(ILogger, LogLevel, object, out string, string, string, int)"/>
        public static bool NullWarning(this ILogger log, object checkValues, string message = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = -1)
        {
            return CheckNull(log, LogLevel.Warning, checkValues, out _, message, method, line);
        }

        /// <inheritdoc cref="CheckNull(ILogger, LogLevel, object, out string, string, string, int)"/>
        public static bool NullError(this ILogger log, object checkValues, string message = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = -1)
        {
            return CheckNull(log, LogLevel.Error, checkValues, out _, message, method, line);
        }

        /// <inheritdoc cref="CheckNull(ILogger, LogLevel, object, out string, string, string, int)"/>
        public static bool NullWarning(this ILogger log, object checkValues, out string nulls, string message = null, [CallerMemberName] string method = "", [CallerLineNumber] int line = -1)
        {
            return CheckNull(log, LogLevel.Warning, checkValues, out nulls, message, method, line);
        }

        /// <summary>
        /// Log if any properties of <paramref name="checkValues"/> are null.
        /// </summary>
        /// <param name="log">this <see cref="ILogger"/></param>
        /// <param name="level">level at which to log</param>
        /// <param name="checkValues">object with properties to check</param>
        /// <param name="nulls">will output the name(s) of any null properties</param>
        /// <param name="message">can be included in warning log, optional</param>
        /// <param name="method">Auto-populated; do not use unless overloading.</param>
        /// <param name="line">Auto-populated; do not use unless overloading.</param>
        /// <returns><c>True</c> if there are any null properties.</returns>
        /// <returns></returns>
        private static bool CheckNull(this ILogger log, LogLevel level, object checkValues, out string nulls, string message, string method, int line)
        {
            if (checkValues is null) return CheckNull(log, level, new { checkValues }, out nulls, message, method, line);

            nulls = checkValues.GetType().GetProperties()
                .Aggregate(new StringBuilder(), (builder, property) => property.GetValue(checkValues) is null ? builder.Append(property.Name).Append(',').Append(' ') : builder)
                .ToString().Trim(',', ' ');
            if (nulls != string.Empty)
            {
                log?.Log(level, "{0}: {1}: Value(s) {2} null at line {3}.", method, message, nulls, line);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> which prints with a prefix
        /// </summary>
        /// <param name="log">Used to create log wrapper</param>
        /// <param name="prefixes">Used to generate prefix</param>
        /// <returns>A new log</returns>
        public static ILogger GetLoggerWithPrefix(this ILogger log, params string[] prefixes)
        {
            return new LogWrapper(log, prefixes);
        }

        /// <summary>
        /// Return an ILogger that prefixes with the function name
        /// </summary>
        /// <remarks>
        /// TODO: This is sort of a hack, and should probably be replaced by use of <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope(string, params object[])"/>.</remarks>
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
