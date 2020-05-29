using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Functions.Configuration;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Functions.Authentication
{
    public static class AuthenticationHelper
    {
        private const int saltBits = 128;
        private const int hashBits = 512;
        private static readonly string jwtSecret = FunctionsConfiguration.Get(ConfigValues.AuthenticationSecret);

        /// <summary>
        /// Hash <paramref name="password"/>. <paramref name="salt"/> is generated when <c>null</c>.
        /// </summary>
        /// <param name="password">Password as received from frontend</param>
        /// <param name="salt">Pass by reference. When null, it will be generated.</param>
        /// <returns></returns>
        public static string Hash(string password, ref string salt)
        {
            byte[] saltByteArray;

            if (salt == null) // then generate salt
            {
                saltByteArray = new byte[saltBits / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltByteArray);
                }
                salt = Convert.ToBase64String(saltByteArray);
            } else // convert salt to byte array
            {
                saltByteArray = Convert.FromBase64String(salt);
                if (saltByteArray.Length != saltBits / 8)
                {
                    throw new FormatException($"Argument {nameof(salt)} with value ${salt} had " +
                        $"improper length or padding. BA size ${saltByteArray.Length}.");
                }
            }

            // derive a 256-bit subkey: HMACSHA512 with 12,000 iterations
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltByteArray,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 12000,
                numBytesRequested: hashBits / 8));
        }

        /// <summary>
        /// Generate a token with an email claim
        /// </summary>
        /// <returns>Token in string form</returns>
        public static string GenerateJwt(ILogger log, string email)
        {
            return GenerateJwt(log, new Dictionary<string, object> { { "email", email } });
        }

        /// <summary>
        /// Generate a token with the given parameters 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="claims">Must contain an "email" claim for most other API calls to work</param>
        /// <param name="expiration"></param>
        /// <param name="fallbackSessionDays">This value is only used if the configuration variable is not found by conventional means</param>
        /// <returns></returns>
        public static string GenerateJwt(ILogger log, IDictionary<string, object> claims = null, 
            DateTimeOffset? expiration = null, double fallbackSessionDays = 2)
        {
            if (!double.TryParse(FunctionsConfiguration.Get(ConfigValues.SessionTokenDays), out double days))
            {
                days = fallbackSessionDays;
                log?.LogWarning($"Invalid value for 'SessionTokenDays'. Defaulting to ${fallbackSessionDays}.");
            }
            claims ??= new Dictionary<string, object>();
            if (!claims.ContainsKey("email")) log.LogWarning("Missing claim 'email'.");
            claims.Add("access", "true"); // Basic claim that is always checked
            claims.Add("exp", (expiration ?? DateTimeOffset.Now.AddDays(days)).ToUnixTimeSeconds());
            return new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(jwtSecret)
                .AddClaims(claims)
                .Encode();
        }

        /// <summary>
        /// Determine if a request is authorized and get the email.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="headers">From a request</param>
        /// <param name="email">Valid when returning <c>True</c></param>
        /// <param name="errorResponse">Valid when returning <c>False</c></param>
        /// <returns></returns>
        public static bool Authorize(ILogger log, IHeaderDictionary headers, out string email, out IStatusCodeActionResult errorResponse)
        {
            IDictionary<string, object> claims = new Dictionary<string, object>();
            if(!Authorize(log, headers, out errorResponse, ref claims))
            {
                email = null;
                return false;
            }

            if(!claims.TryGetValue("email", out var emailObj)) {
                log.LogWarning("Missing email claim at Authorize.");
                errorResponse = new UnauthorizedResult();
            }

            return !string.IsNullOrEmpty(email = (string)emailObj);
            
            //return d.TryGetValue("email", out var emailObj) && (email = emailObj as string);

        }

        /// <summary>
        /// Determine if a request is authorized based on its header, with only default claims.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="headers">Request headers</param>
        /// <param name="errorResponse"> When the return value is false, errorResponse is set to an ObjectResult
        /// which can be returned in an MVC-style method </param>
        /// <returns>True if authorized</returns>
        public static bool Authorize(ILogger log, IHeaderDictionary headers, out IStatusCodeActionResult errorResponse)
        {
            IDictionary<string, object> d = new Dictionary<string, object>();
            return Authorize(log, headers, out errorResponse, ref d);
        }

        /// <summary>
        /// Determine if a response is authorized based on its headers and validating any additional <paramref name="claims"/>.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="headers">Request headers</param>
        /// <param name="errorResponse">When the return value is false, errorResponse is set to an <see cref="ActionResult"/>
        /// which can be returned in an MVC-style method </param>
        /// <param name="claims">Used to pass in additional claims to verify; when validation is successful, 
        /// all claims are returned (not just any passed in) </param>
        /// <returns></returns>
        public static bool Authorize(ILogger log, IHeaderDictionary headers, out IStatusCodeActionResult errorResponse, 
            ref IDictionary<string, object> claims)
        {
            var authStart = "Bearer ";
            if (!headers.TryGetValue("Authorization", out var val) || val.Count != 1 || !val[0].StartsWith(authStart))
            {
                errorResponse = new UnauthorizedObjectResult("No credentials.");
                return false;
            }
            var jwt = val[0].Remove(0, authStart.Length);
            switch (ValidateJwt(log, jwt, ref claims))
            {
                case JwtValidationResult.Valid:
                    // This response should NOT be used or returned to the caller. 
                    // This could be improved by with an ActionResult derivative which throws an exception when ExecuteResultAsync is called
                    errorResponse = null;
                    return true;
                case JwtValidationResult.Expired: 
                    errorResponse = new UnauthorizedObjectResult("Expired token.");
                    return false;
                case JwtValidationResult.InvalidSignature:
                case JwtValidationResult.MissingOrInvalidClaim:
                default:
                    errorResponse = new UnauthorizedResult(); // We log the result, but no need to tell the caller why
                    return false;
            }
        }

        public enum JwtValidationResult
        {
            Expired,
            InvalidSignature,
            MissingOrInvalidClaim,
            Valid
        }

        /// <summary>
        /// Validate a JWT token with <paramref name="claims"/>
        /// </summary>
        /// <param name="token"></param>
        /// <param name="claims"> Pass in claims to validate; pass out all claims on the token. </param>
        /// <param name="log"></param>
        /// <returns>true if the token has a valid signature, is not expired, and matches any claims in 'claims'</returns>
        /// <remarks> Ideally, this method and JwtValidationResult enum should be internal or private, but 
        /// I don't like the unit testing options then. It's also possible there are legitimate cases for 
        /// ValidateJwt to be called from an Azure Function. 
        /// </remarks>
        public static JwtValidationResult ValidateJwt(ILogger log, string token, 
            ref IDictionary<string, object> claims)
        {
            var claimsToValidate = claims ?? new Dictionary<string, object>();
            if (!claims.ContainsKey("access")) claimsToValidate.Add("access", "true");

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
                return JwtValidationResult.Expired;
            }
            catch (SignatureVerificationException)
            {
                log.LogWarning("Token has invalid signature.");
                return JwtValidationResult.InvalidSignature;
            }

            foreach (var c in claimsToValidate)
            {
                if (!claims.TryGetValue(c.Key, out var val) || !val.Equals(c.Value))
                {
                    log.LogWarning($"Missing or invalid claim '{c.Key}'");
                    return JwtValidationResult.MissingOrInvalidClaim;
                }
            }

            return JwtValidationResult.Valid;
        }

    }
}
