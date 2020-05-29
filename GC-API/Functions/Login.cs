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

            if (log.NullWarning(new { email, password }, out string nullNames))
                return new BadRequestObjectResult($"Missing parameter(s) {nullNames}");

            if(!email.ToLower().TryEncodeBase64(out var email64))
                return new BadRequestObjectResult($"Invalid email.");

            if (!client.WrapCall(log, x => x.ReadDocumentAsync<GcUser>($"dbs/userdb/colls/usercoll/docs/{email64}")).GetWrapResult(out var statusCode, out var response))
                return new StatusCodeResult((int)statusCode);
            
            var user = response.Document;
            var salt = user.salt;
            if (user.hash == AuthenticationHelper.Hash(password, ref salt))
            {
                var uiUser = ModelConverter.Convert<UiUser>(user);
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
