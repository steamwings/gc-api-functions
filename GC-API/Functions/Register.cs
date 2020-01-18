using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Functions
{
    public static class Register
    {
        [FunctionName("Register")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing register request...");

            string name = req.Query["name"];
            string username = req.Query["username"];
            string password = req.Query["password"]; //presalted

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            name = name ?? data?.name;
            username = username ?? data?.username;
            password = password ?? data?.password;

            if (name is null || username is null || password is null) return new BadRequestObjectResult("Missing parameter.");

            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            // derive a 256-bit subkey: HMACSHA1 with 10,000 iterations
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            //TODO Check for existing user (even though the UI should tell you this too)

            //return new ...RequestObjectResult("Username already exists.")

            //TODO: Write to Cosmos

            return password != null
                ? (ActionResult)new OkObjectResult($"Hello, {username}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
