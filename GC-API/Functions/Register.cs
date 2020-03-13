using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Client;
using System.Net;
using Functions.Authentication;
using Models.User;
using Microsoft.Azure.Documents;

namespace Functions
{
    /// <summary>
    /// A class for the Register Azure function
    /// </summary>
    public static class Register
    {
        [FunctionName("Register")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [CosmosDB(
                databaseName: "userdb",
                collectionName: "usercoll",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation($"Processing register request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string name = data?.name;
            string email = data?.email;
            string password = data?.password; // should be presalted by front-end

            string nullName = 0 switch { // Obtuse null check
                _ when name is null => nameof(name),
                _ when email is null => nameof(email),
                _ when password is null => nameof(password),
                _ => "none"
            };

            if (nullName != "none") {
                log.LogWarning($"Parameter {nullName} cannot be null.");
                return new BadRequestObjectResult($"Missing parameter ${nullName}.");
            }

            string salt = null; // assignment required because it's passed with ref
            string hash;
            try
            {
                hash = AuthenticationHelper.Hash(password, ref salt);
                log.LogTrace($"Generated hash: {hash}, salt: {salt}");
            }
            catch (FormatException e)
            {
                log.LogError(e, "Invalid password hash.");
                return new BadRequestObjectResult("Invalid password hash.");
            }

            var coreUser = new CoreUser { email = email, name = name };
            var user = new GcUser { hash = hash, salt = salt, coreUser = coreUser};
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            try
            {
                var resp = await client.CreateDocumentAsync("dbs/userdb/colls/usercoll/", user);
                statusCode = resp.StatusCode;
                if (statusCode == HttpStatusCode.Created)
                {
                    var token = AuthenticationHelper.GenerateJwt(log, email);
                    return new CreatedResult(resp.Resource.Id, new { coreUser.name, token });
                }
            } catch (DocumentClientException e)
            {
                switch (statusCode)
                {
                    case HttpStatusCode.TooManyRequests:
                        log.LogCritical("Register got 429!");
                        break;
                    case HttpStatusCode.Forbidden: 
                        log.LogCritical("Permissions issue or collection full!");
                        break;
                    default: 
                        log.LogError(e, $"Register func got error ${e.StatusCode} for CreateDoc."); 
                        break;
                }
                statusCode = e.StatusCode ?? HttpStatusCode.InternalServerError;
            } finally
            {
                log.LogDebug($"Register status {(int) statusCode} for user: {JsonConvert.SerializeObject(user)}");
            }
            return new StatusCodeResult((int) statusCode);
        }
    }
}
