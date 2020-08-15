using System;
using System.ComponentModel;
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
                encodedStr = System.Convert.ToBase64String(encoding.GetBytes(str));
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
                decodedStr = encoding.GetString(System.Convert.FromBase64String(str));
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
                return encoding.GetString(System.Convert.FromBase64String(str));
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

        // TODO Remove?
        ///// <summary>
        ///// Attempt to parse a double and return a double even on failure
        ///// </summary>
        ///// <param name="str"></param>
        ///// <param name="fallback"></param>
        ///// <returns>The parsed double or <paramref name="fallback"/></returns>
        //public static double ToDouble(this string str, double fallback = double.NaN)
        //{
        //    return double.TryParse(str, out var result)
        //        ? result
        //        : fallback;
        //}

        ///// <summary>
        ///// Attempt to parse an int and return an int even on failure
        ///// </summary>
        ///// <param name="str"></param>
        ///// <param name="fallback"></param>
        ///// <returns></returns>
        //public static int ToInt(this string str, int fallback = int.MinValue)
        //{
        //    return int.TryParse(str, out var result)
        //        ? result
        //        : fallback;
        //}

        /// <summary>
        /// Parse a string; if parsing fails, return the <paramref name="fallback"/> value
        /// </summary>
        /// <typeparam name="T">Type to parse to, detected from <paramref name="fallback"/></typeparam>
        /// <param name="str">this string to parse</param>
        /// <param name="fallback">Value to return if parsing fails</param>
        /// <returns>The parsed <typeparamref name="T"/> value or <paramref name="fallback"/></returns>
        /// <remarks> This is intended to work for numeric types like int and double. </remarks>
        public static T ParseWithDefault<T>(this string str, T fallback)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.IsValid(str))
                return (T) converter.ConvertFromString(str);
            
            return fallback;
        }
    }
}
