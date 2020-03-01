using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Logging;
using JWT;
using Microsoft.AspNetCore.Http;

namespace Functions.Authentication
{
    public static class AuthenticationHelper
    {
        private const int saltBits = 128;
        private const int hashBits = 512;
        private static readonly string jwtSecret = Environment.GetEnvironmentVariable("AuthenticationSecret");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt">Pass by reference. When null, it will be generated.</param>
        /// <returns></returns>
        public static string Hash(string password, ref string salt)
        {
            byte[] saltBA;

            if (salt == null) // then generate salt
            {
                saltBA = new byte[saltBits / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBA);
                }
                salt = Convert.ToBase64String(saltBA);
            } else // convert salt to byte array
            {
                saltBA = Convert.FromBase64String(salt);
                if (saltBA.Length != saltBits / 8)
                {
                    throw new FormatException($"Argument {nameof(salt)} with value ${salt} had " +
                        $"improper length or padding. BA size ${saltBA.Length}.");
                }
            }

            // derive a 256-bit subkey: HMACSHA512 with 12,000 iterations
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBA,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 12000,
                numBytesRequested: hashBits / 8));
        }

        public static string GenerateJwt(string email, ILogger log)
        {
            return GenerateJwt(new Dictionary<string, object> { { "email", email } }, log);
        }
        public static string GenerateJwt(Dictionary<string, object> claims, ILogger log)
        {
            if (!int.TryParse(Environment.GetEnvironmentVariable("SessionTokenDays"), out int days))
            {
                log?.LogWarning("Invalid value for 'SessionTokenDays' (should be an integer)");
                days = 4;
            }
            claims.Add("access", "true"); // Basic claim that is always checked
            claims.Add("exp", DateTimeOffset.Now.AddDays(days).ToUnixTimeSeconds());
            return new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(jwtSecret)
                .AddClaims(claims)
                .Encode();
        }

        public static bool Authorize(IHeaderDictionary headers, ILogger log)
        {
            var authStart = "Bearer ";
            if (!headers.TryGetValue("Authorization", out var val) || val.Count != 1 || !val[0].StartsWith(authStart))
                return false;
            return ValidateJwt(val[0].Remove(0, authStart.Length), log);
        }

        /// <summary>
        /// Validate a JWT with only default claims
        /// </summary>
        /// <param name="token"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static bool ValidateJwt(string token, ILogger log)
        {
            IDictionary<string, object> d = new Dictionary<string, object>();
            return ValidateJwt(token, ref d, log);
        }

        /// <summary>
        /// Validate a JWT token and check that it has an email claim matching 'email'. Ignores other claims.
        /// </summary>
        /// <returns>true if validated</returns>
        public static bool ValidateJwt(string token, string email, ILogger log)
        {
            IDictionary<string, object> d = new Dictionary<string, object> { { "email", email } };
            return ValidateJwt(token, ref d, log);
        }

        /// <summary>
        /// Validate a JWT token with particular claims
        /// </summary>
        /// <param name="token"></param>
        /// <param name="claims"> Pass in claims to validate; passes out all claims on the token. </param>
        /// <param name="log"></param>
        /// <returns>true if the token has a valid signature, is not expired, and matches any claims in 'claims'</returns>
        public static bool ValidateJwt(string token, ref IDictionary<string, object> claims, ILogger log)
        {
            var claimsToValidate = claims ?? new Dictionary<string, object>();
            claimsToValidate.Add("access", "true");

            try
            {
                claims = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(jwtSecret)
                    .MustVerifySignature()
                    .Decode<IDictionary<string, object>>(token);
            }
            catch (TokenExpiredException)
            {
                log.LogWarning("Expired token.");
                return false;
            }
            catch (SignatureVerificationException)
            {
                log.LogWarning("Token has invalid signature.");
                return false;
            }

            foreach (var c in claimsToValidate)
            {
                if (!claims.TryGetValue(c.Key, out var val) || !val.Equals(c.Value))
                    return false;
            }

            return true;
        }

    }
}
