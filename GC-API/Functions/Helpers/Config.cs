using Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Functions.Helpers
{
    /// <summary>
    /// Defines keys for available configuration options.
    /// </summary>
    /// <remarks>
    /// The name of the Enum value must match the string used in configuration JSON. 
    /// </remarks>
    public enum ConfigKeys
    {
        AuthenticationSecret,
        SessionTokenDays,
        ProfilePicUploadSasExpiryHours,
        ProfilePicDefaultSasExpiryHours,
        ProfileWidthLarge,
        ProfileWidthMedium,
        ProfileWidthSmall,
    }

    /// <summary>
    /// Provides configuration-related functionality.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Get a configuration string value.
        /// </summary>
        /// <param name="key">The <see cref="ConfigKeys"/> </param>
        /// <returns>Configuration value</returns>
        public static string Get(ConfigKeys key)
        {
            return Environment.GetEnvironmentVariable(key.ToString());
        }

        /// <summary>
        /// Get a configuration value and parse it to a convertible type
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="key">The <see cref="ConfigKeys"/> </param>
        /// <param name="fallback">Value to use when parsing goes wrong or no converter is found</param>
        /// <returns>Configuration value</returns>
        public static T Get<T>(ConfigKeys key, T fallback) where T : IConvertible
        {
            return Environment.GetEnvironmentVariable(key.ToString())
                .ParseWithDefault(fallback);
        }
    }
}
