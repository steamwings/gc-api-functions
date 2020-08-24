using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            [Blob("profile-pics/{id}", Connection = "SharedStorage")] CloudBlobContainer container,
            string id,
            ILogger log)
        {
            log = log.GetLoggerWithPrefix(nameof(PictureUploadUrl));
            log.LogTrace("Processing request...");

            if (!AuthenticationHelper.Authorize(log, req.Headers, out var authId, out var errorResponse))
                return errorResponse;

            if (id != authId) 
                return new UnauthorizedResult();

            if (!StorageHelper.TryGetServiceSas(log, out var sasUrl, container.Name, container.Uri.ToString(),
                accountName: Config.Get(ConfigKeys.SharedStorageAccountName),
                accountKey: Config.Get(ConfigKeys.SharedStorageKey),
                hours: Config.Get(ConfigKeys.ProfilePicUploadSasExpiryHours).ParseWithDefault(.1),
                permissions: Config.Get(ConfigKeys.ProfilePicUploadSasPermissions).ParseWithDefault(1),
                blobName: id))
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);

            return new OkObjectResult(sasUrl);
        }
    }
}
