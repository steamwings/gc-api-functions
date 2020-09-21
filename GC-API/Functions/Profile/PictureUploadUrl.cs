using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Common.Extensions;
using Functions.Helpers;
using Microsoft.Azure.Storage.Blob;
using System.Reflection.Metadata;

namespace Functions.Profile
{
    /// <summary>
    /// Return a SAS URL at which to upload a profile picture
    /// </summary>
    public static class PictureUploadUrl
    {
        [FunctionName("PictureUploadUrl")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "profile/upload-pic-url/{id}")] HttpRequest req,
            [Blob("profile-pics/{id}", Connection = "SharedUserStorage")] CloudBlockBlob blob,
            string id,
            ILogger log)
        {
            log = log.GetLoggerWithPrefix(nameof(PictureUploadUrl));
            log.LogTrace("Processing request...");

            if (!AuthenticationHelper.Authorize(log, req.Headers, out var authId, out var errorResponse))
                return errorResponse;

            if (id != authId) 
                return new UnauthorizedResult();

            var policy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Write,
                SharedAccessExpiryTime = DateTimeOffset.Now.AddHours(Config.Get(ConfigKeys.ProfilePicUploadSasExpiryHours).ParseWithDefault(.5)),
                SharedAccessStartTime = DateTimeOffset.Now.AddMinutes(-2)
            };

            return new OkObjectResult(blob.Uri + blob.GetSharedAccessSignature(policy));
        }
    }
}
