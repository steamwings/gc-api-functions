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
    }

    /// <summary>
    /// Provides configuration-related functionality.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Wrapper method to get a configuration value.
        /// </summary>
        /// <param name="key">The <see cref="ConfigKeys"/> </param>
        /// <returns></returns>
        public static string Get(ConfigKeys key)
        {
            return Environment.GetEnvironmentVariable(key.ToString());
        }
    }
}
