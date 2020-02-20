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
using Microsoft.Azure.Documents;

namespace Functions
{
    public static class Register
    {
        [FunctionName("Register")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "UsersDB",
                collectionName: "Users",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("Processing register request...");

            string name = req.Query["name"];
            string email = req.Query["email"];
            string password = req.Query["password"]; //presalted

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

            if (name != "none") {
                log.LogWarning($"Parameter {nullName} cannot be null.");
                return new BadRequestObjectResult($"Missing parameter ${nullName}.");
            }

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

            try
            {
                var user = await client.CreateUserAsync("UserDB", new User { Id = email });
            } catch (DocumentClientException e)
            {
                log.LogError(e, "Create user failed.");
            }

            log.LogInformation("Hash: " + hashed);

            //TODO: Write to Cosmos

            return password != null
                ? (ActionResult) new OkObjectResult($"Hello, {email}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
