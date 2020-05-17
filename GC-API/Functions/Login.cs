using Functions.Extensions;
using Functions.Authentication;
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

namespace Functions
{
    /// <summary>
    /// Class for the Login Azure Function
    /// </summary>
    public static class Login
    {
        [FunctionName("Login")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [CosmosDB("userdb","usercoll",
            ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogTrace("Processing register request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string email = data?.email;
            string password = data?.password;
            string userDocId = data?.userId;

            string nullName = 0 switch // Obtuse null check
            {
                _ when email is null => nameof(email),
                _ when password is null => nameof(password),
                _ => "none"
            };

            if (nullName != "none")
            {
                log.LogWarning($"Parameter {nullName} cannot be null.");
                return new BadRequestObjectResult($"Missing parameter ${nullName}.");
            }

            GcUser user = null; // We need to get the user's salt
            if(userDocId != null) // With the document id, we can read the document directly
            {
                var resp = await client.ReadDocumentAsync<GcUser>($"dbs/userdb/colls/usercoll/docs/{userDocId}");
                if (resp.IsSuccessStatusCode())
                {
                    user = resp.Document;
                    if(user.coreUser.email != email)
                    {
                        log.LogError("Email mismatch for user retrieved with userDocId!");
                        log.LogDebug($"DocId: {userDocId} | Email in doc: {user.coreUser.email} | Email in request: {email}");
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                } 
                else 
                {
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            log.LogWarning("A request which included the userDocId got a 404 for that document.");
                            break; // We can try searching for the user
                        case HttpStatusCode.TooManyRequests:
                            log.LogCritical("Request denied due to lack of RUs (429)!");
                            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                        default:
                            log.LogInformation($"Cosmos ReadDoc call failed with {resp.StatusCode}");
                            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }
                }
            }
            if (user is null) {// Without the document id, we need to find the user
                var users = client.CreateDocumentQuery<GcUser>("dbs/userdb/colls/usercoll")
                    .Where(u => u.coreUser.email == email);
                switch (users.Count())
                {
                    case 0:
                        log.LogTrace($"User for ${email} was not found.");
                        return new NotFoundResult();
                    case 1: break;
                    default:
                        log.LogCritical($"More than one user with email '{email}'");
                        return new StatusCodeResult(500);
                }
                user = users.AsEnumerable().Single();
            }

            string salt = user.salt;
            if (user.hash == AuthenticationHelper.Hash(password, ref salt))
            {
                var token = AuthenticationHelper.GenerateJwt(log, email);
                return new OkObjectResult(new { user.coreUser.name, token });
            } else {
                // TODO Save failed login attempts somewhere so we can notify users
                log.LogWarning($"Failed login attempt for {email}");
                return new UnauthorizedResult();
            }
        }
    }
}
