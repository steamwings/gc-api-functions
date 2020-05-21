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
using Microsoft.Azure.Documents;
using Models.Common.User;
using Models.Database.User;
using Common.Extensions;

namespace Functions
{
    /// <summary>
    /// A class for the Register Azure function
    /// </summary>
    public static class Register
    {
        [FunctionName(nameof(Register))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [CosmosDB(
                databaseName: "userdb",
                collectionName: "usercoll",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation($"{nameof(Register)}: processing request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string name = data?.name;
            string email = data?.email;
            string password = data?.password; // should be presalted by front-end

            if(log.NullWarning(new {name, email, password}, out string nullNames))
                return new BadRequestObjectResult($"Missing parameter(s) {nullNames}");
            
            if (!email.TryConvertToBase64(out var email64))
                return new BadRequestObjectResult($"Invalid email.");

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

            var coreUser = new UserCore { email64 = email64, name = name };
            var user = new GcUser { hash = hash, salt = salt, userCore = coreUser };
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
                        log.LogCritical($"{nameof(Register)}: got 429!");
                        break;
                    case HttpStatusCode.Forbidden: 
                        log.LogCritical($"{nameof(Register)}: got permissions issue or collection full!");
                        break;
                    default: 
                        log.LogError(e, $"{nameof(Register)}: got error ${e.StatusCode} for CreateDoc."); 
                        break;
                }
                statusCode = e.StatusCode ?? HttpStatusCode.InternalServerError;
            } finally
            {
                log.LogDebug($"{nameof(Register)}: status {(int) statusCode} for user: {JsonConvert.SerializeObject(user)}");
            }
            return new StatusCodeResult((int) statusCode);
        }
    }
}
