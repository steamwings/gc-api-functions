using Functions.Helpers;
using Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Models.Database.User;
using Models;
using Models.UI.User;

namespace Functions.Primitives
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
            log = log.GetLoggerWithPrefix(nameof(Login));
            log.LogTrace("Processing request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string email = data?.email;
            string password = data?.password;
            string id = data?.id;

            if (log.NullWarning(new { email, password }, out string nullNames))
                return new BadRequestObjectResult($"Missing parameter(s) {nullNames}");

            GcUser user;
            if (id is null)
            {
                if(!client.FindUniqueItem(log,
                    x => x.CreateDocumentQuery<GcUser>("dbs/userdb/colls/usercoll")
                        .Where(u => u.userCore.email == email),
                    out user, out var errorResponse)) {
                    return errorResponse;
                }
            }
            else // if id provided
            {
                if (!client.WrapCall(log, x => x.ReadDocumentAsync<GcUser>($"dbs/userdb/colls/usercoll/docs/{id}")).GetWrapResult(out var statusCode, out var response))
                    return new StatusCodeResult((int)statusCode);

                user = response.Document;
            }

            var salt = user.salt;
            if (user.hash == AuthenticationHelper.Hash(password, ref salt))
            {
                var uiUser = ModelConverter.Convert<UiUser>(user);
                uiUser.token = AuthenticationHelper.GenerateJwt(log, user.id);
                return new OkObjectResult(uiUser);
            } else {
                // TODO Save failed login attempts somewhere so we can notify users
                log.LogWarning($"Failed login attempt for {email}");
                return new UnauthorizedResult();
            }
        }
    }
}
