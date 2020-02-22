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
using Microsoft.Azure.Documents.Client;
using System.Net;

namespace Functions
{
    /// <summary>
    /// A class for the Register Azure function
    /// </summary>
    public static class Register
    {
        [FunctionName("Register")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "userdb",
                collectionName: "usercoll",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("Processing register request...");

            string name = req.Query["name"];
            string email = req.Query["email"];
            string password = req.Query["password"]; // presalted

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            name ??= data?.name;
            email ??= data?.email;
            password ??= data?.password;

            string nullName = 0 switch {
                _ when name is null => nameof(name),
                _ when email is null => nameof(email),
                _ when password is null => nameof(password),
                _ => "none"
            };

            if (nullName != "none") {
                log.LogWarning($"Parameter {nullName} cannot be null.");
                return new BadRequestObjectResult($"Missing parameter ${nullName}. You can use query string or request body.");
            }

            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            var saltString = Convert.ToBase64String(salt);
            Console.WriteLine($"Salt: {saltString}");

            // derive a 256-bit subkey: HMACSHA1 with 10,000 iterations
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            log.LogInformation("Hash: " + hashed);

            var resp = await client.CreateDocumentAsync("dbs/userdb/colls/usercoll/", new { hash = hashed, salt = saltString }) ; 
            
            return resp.StatusCode == HttpStatusCode.Created
                ? (IActionResult) new CreatedResult(resp.Resource.SelfLink, resp.Resource)
                : new StatusCodeResult((int)resp.StatusCode);
        }
    }
}
