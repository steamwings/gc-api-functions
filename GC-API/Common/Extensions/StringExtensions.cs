using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Common.Extensions
{
    public static class StringExtensions
    {
        private static readonly Encoding encoding = new UTF8Encoding(false, true);

        /// <summary>
        /// Try to encode a string to base64
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Base64 string</returns>
        public static bool TryEncodeBase64(this string str, out string encodedStr)
        {
            try
            {
                encodedStr = Convert.ToBase64String(encoding.GetBytes(str));
                return true;
            }
            catch(EncoderFallbackException)
            {
                // log
            }
            catch (Exception)
            {
                // log
            }
            encodedStr = string.Empty;
            return false;
        }

        /// <summary>
        /// Try to decode a base64 string
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Base64 decoded string</returns>
        public static bool TryDecodeBase64(this string str, out string decodedStr)
        {
            try
            {
                decodedStr = encoding.GetString(Convert.FromBase64String(str));
                return true;
            }
            catch (DecoderFallbackException)
            {
                // log
            }
            catch (Exception)
            {
                // log
            }
            decodedStr = string.Empty;
            return false;
        }

        /// <summary>
        /// Attempt to convert from base64, but always return a string.
        /// </summary>
        /// <param name="str">Base64-encoded string</param>
        /// <param name="returnOnFail">The string to return when decoding fails. When null, returns a message including <paramref name="str"/> on failure.</param>
        /// <returns></returns>
        public static string DecodeBase64(this string str, string returnOnFail = null)
        {
            try
            {
                return encoding.GetString(Convert.FromBase64String(str));
            } catch
            {
                return returnOnFail ?? $"{str}_decodeFailed";
            }
        }

        /// <summary>
        /// Try to deserialize a JSON string.
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="str">this</param>
        /// <param name="deserialized">output</param>
        /// <returns><c>True</c> when successful.</returns>
        public static bool TryDeserialize<T>(this string str, out T deserialized)
        {
            try
            {
                deserialized = JsonSerializer.Deserialize<T>(str);
                return true;
            }
            catch (JsonException)
            {
                deserialized = default;
                return false;
            }
        }
    }
}
