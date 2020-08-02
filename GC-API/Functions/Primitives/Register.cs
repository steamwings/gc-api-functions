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
using Models;
using Models.UI.User;

namespace Functions.Primitives
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
            log = log.GetLoggerWithPrefix(nameof(Register));
            log.LogTrace("Processing request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string name = data?.name;
            string email = data?.email;
            string password = data?.password; // should be presalted by front-end

            if(log.NullWarning(new {name, email, password}, out string nullNames))
                return new BadRequestObjectResult($"Missing parameter(s) {nullNames}");
           
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

            var coreUser = new UserCore { name = name, email = email };
            var user = new GcUser { hash = hash, salt = salt, userCore = coreUser, profile = new UserProfile() };

            if(client.WrapCall(log, x => x.CreateDocumentAsync("dbs/userdb/colls/usercoll/", user)).GetWrapResult(out var statusCode, out var response))
            {
                user.id = response.Resource.Id;
                var uiUser = ModelConverter.Convert<UiUser>(user);
                uiUser.token = AuthenticationHelper.GenerateJwt(log, response.Resource.Id);
                return new ObjectResult(uiUser) { StatusCode = 201 };
            }
            else return new StatusCodeResult((int)statusCode);

        }
    }
}
