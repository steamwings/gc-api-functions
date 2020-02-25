using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Functions.Helpers
{
    public static class HashHelper
    {
        private const int saltBits = 128;
        private const int hashBits = 512;
        public static string Hash(string password, ref string salt)
        {
            byte[] saltBA;

            if (salt == null) // Generate salt
            {
                saltBA = new byte[saltBits / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBA);
                }
                salt = Convert.ToBase64String(saltBA);
                //log.LogDebug($"Salt: {salt}");
            } else // Convert salt to byte array
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
                numBytesRequested: hashBits / 8)); // 256 bit
        }
    }
}
