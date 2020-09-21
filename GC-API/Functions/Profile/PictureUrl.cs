using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Common.Extensions;
using Functions.Helpers;
using Microsoft.Azure.Storage.Blob;

namespace Functions.Profile
{
    /// <summary>
    /// Return a SAS URL at which to get profile pictures
    /// </summary>
    public static class PictureUrl
    {
        [FunctionName("PictureUrl")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "profile/pic-url")] HttpRequest req,
            [Blob("profile-pics", Connection = "SharedUserStorage")] CloudBlobContainer container,
            ILogger log)
        {
            log = log.GetLoggerWithPrefix(nameof(PictureUrl));
            log.LogTrace("Processing request...");

            if (!AuthenticationHelper.Authorize(log, req.Headers, out var errorResponse))
                return errorResponse;

            var policy = new SharedAccessBlobPolicy() {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.Now.AddHours(Config.Get(ConfigKeys.ProfilePicDefaultSasExpiryHours).ParseWithDefault(.5)),
                SharedAccessStartTime = DateTimeOffset.Now.AddMinutes(-2)
            };

            return new OkObjectResult(container.Uri + container.GetSharedAccessSignature(policy));
        }
    }
}
