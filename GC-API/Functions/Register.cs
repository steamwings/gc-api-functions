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
            
            if (!email.ToLower().TryEncodeBase64(out var email64))
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

            var coreUser = new UserCore { name = name };
            var user = new GcUser { id = email64, hash = hash, salt = salt, userCore = coreUser };

            if(client.WrapCall(log, x => x.CreateDocumentAsync("dbs/userdb/colls/usercoll/", user)).GetWrapResult(out var statusCode, out var response))
            {
                var token = AuthenticationHelper.GenerateJwt(log, email);
                return new CreatedResult(response.Resource.Id, new { coreUser.name, token });
            }
            else return new StatusCodeResult((int)statusCode);

        }
    }
}
