using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Models.Common.User;
using Microsoft.Azure.Documents.Client;
using Common.Extensions;
using Models.Database.User;
using Functions.Authentication;
using System.Web.Http;

namespace Functions
{
    public static class ProfileUpdate
    {
        [FunctionName(nameof(ProfileUpdate))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "profile")] HttpRequest req,
            [CosmosDB(
                databaseName: "userdb",
                collectionName: "usercoll",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogTrace($"{nameof(ProfileUpdate)}: processing request...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if(!requestBody.TryDeserialize<UserProfile>(out var updatedProfile))
                return new BadRequestObjectResult("Invalid profile.");

            if (!AuthenticationHelper.Authorize(log, req.Headers, out var email, out var errorResponse))
                return errorResponse;

            if (!email.ToLower().TryEncodeBase64(out var email64))
            {
                log.LogError($"Encoding failed for email {email} in JWT token!");
                return new InternalServerErrorResult();
            }

            var link = $"dbs/userdb/colls/usercoll/docs/{email64}";

            if (!client.WrapCall(log, x => x.ReadDocumentAsync<GcUser>(link)).GetWrapResult(out var statusCode, out var response))
                return new StatusCodeResult((int)statusCode);

            response.Document.profile = updatedProfile;
            client.WrapCall(log, x => x.UpsertDocumentAsync(link, response.Document)).GetWrapResult(out statusCode, out _);

            return new StatusCodeResult((int)statusCode);
        }
    }
}
