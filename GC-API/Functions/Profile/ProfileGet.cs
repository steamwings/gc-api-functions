using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Documents.Client;
using Common.Extensions;
using Functions.Authentication;
using Models.Database.User;

namespace Functions.Profile
{
    public static class ProfileGet
    {
        [FunctionName(nameof(ProfileGet))]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "profile/{id}")] HttpRequest req,
            [CosmosDB(
                databaseName: "userdb",
                collectionName: "usercoll",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            string id,
            ILogger log)
        {
            log.LogInformation("ProfileGet running."); // TODO Remove (should be unnecessary)
            log = log.GetLoggerWithPrefix(nameof(ProfileGet));
            log.LogTrace("Processing request...");

            if (!AuthenticationHelper.Authorize(log, req.Headers, out var errorResponse))
                return errorResponse;
            log.LogTrace("Authorized.");

            var link = $"dbs/userdb/colls/usercoll/docs/{id}";
            if (!client.WrapCall(log, x => x.ReadDocumentAsync<GcUser>(link)).GetWrapResult(out var statusCode, out var response))
                return new StatusCodeResult((int)statusCode);
            log.LogTrace("ReadDocAsync succeeded.");
             
            return new OkObjectResult(response.Document.profile);
        }
    }
}
