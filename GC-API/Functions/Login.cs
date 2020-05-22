using Functions.Authentication;
using Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Models.Database.User;
using System.Text;
using Models;
using Models.Common.User;
using Models.UI.User;

namespace Functions
{
    /// <summary>
    /// Class for the Login Azure Function
    /// </summary>
    public static class Login
    {
        [FunctionName(nameof(Login))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [CosmosDB(
                databaseName: "userdb",
                collectionName: "usercoll", 
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogTrace($"{nameof(Login)}: processing request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string email = data?.email;
            string password = data?.password;
            string userDocId = data?.userId;

            if (log.NullWarning(new { email, password }, out string nullNames))
                return new BadRequestObjectResult($"Missing parameter(s) {nullNames}");

            if(!email.TryConvertToBase64(out var email64))
                return new BadRequestObjectResult($"Invalid email.");

            GcUser user = null; // We need to get the user's salt

            var resp = await client.ReadDocumentAsync<GcUser>($"dbs/userdb/colls/usercoll/docs/{email64}");
            if (resp.IsSuccessStatusCode())
            {
                user = resp.Document;
                if(user.id != email64)
                {
                    log.LogError("Email mismatch for user retrieved with userDocId!");
                    log.LogDebug($"DocId: {userDocId} | Email in doc: {user.id.FromBase64()} | Email in request: {email}");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            } 
            else 
            {
                switch (resp.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return new NotFoundResult();
                    case HttpStatusCode.TooManyRequests:
                        log.LogCritical("Request denied due to lack of RUs (429)!");
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    default:
                        log.LogInformation($"Cosmos ReadDoc call failed with {resp.StatusCode}");
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            
            string salt = user.salt;
            if (user.hash == AuthenticationHelper.Hash(password, ref salt))
            {
                var uiUser = ModelConverter.Convert<UserCoreUI>(user);
                uiUser.token = AuthenticationHelper.GenerateJwt(log, email);
                return new OkObjectResult(uiUser);
            } else {
                // TODO Save failed login attempts somewhere so we can notify users
                log.LogWarning($"Failed login attempt for {email}");
                return new UnauthorizedResult();
            }
        }
    }
}
