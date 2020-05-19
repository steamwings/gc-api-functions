using System;
using System.Collections.Generic;
using System.Text;

namespace Functions.Configuration
{
    /// <summary>
    /// Defines available configuration options.
    /// </summary>
    /// <remarks>
    /// The name of the Enum value must match the string used in configuration JSON. 
    /// </remarks>
    public enum ConfigValues
    {
        AuthenticationSecret,
        SessionTokenDays
    }

    /// <summary>
    /// Provides configuration-related functionality.
    /// </summary>
    public static class FunctionsConfiguration
    {
        /// <summary>
        /// Wrapper method to get a configuration value.
        /// </summary>
        /// <param name="key">The <see cref="ConfigValues"/> </param>
        /// <returns></returns>
        public static string Get(ConfigValues key)
        {
            return Environment.GetEnvironmentVariable(key.ToString());
        }
    }
}
