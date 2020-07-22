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
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "profile/{id}")] HttpRequest req,
            string id,
            [CosmosDB(
                databaseName: "userdb",
                collectionName: "usercoll",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log = log.GetLoggerWithPrefix(nameof(ProfileGet));
            log.LogTrace("Processing request...");

            if (!AuthenticationHelper.Authorize(log, req.Headers, out var errorResponse))
                return errorResponse;

            var link = $"dbs/userdb/colls/usercoll/docs/{id}";
            var result = await client.WrapCall(log, x => x.ReadDocumentAsync<GcUser>(link));
            if (!result.Success)
                return new StatusCodeResult((int)result.StatusCode);

            return new OkObjectResult(result.Response.Document.profile);
        }
    }
}
