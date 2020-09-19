using Azure.Storage;
using Azure.Storage.Sas;
using Common.Extensions;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Functions.Helpers
{
    /// <summary>
    /// Static methods for Azure Storage management
    /// </summary>
    /// <remarks>
    /// We're using the .NET v11 SDK (where we have to)
    /// </remarks>
    public static class StorageHelper
    {
        /// <summary>
        /// Create a Shared Access Signature url for a container or blob
        /// </summary>
        /// <param name="log"></param>
        /// <param name="containerName"></param>
        /// <param name="uri">of the container or blob</param>
        /// <param name="accountName">Name of storage account</param>
        /// <param name="accountKey">Storage account key</param>
        /// <param name="hours">until expiration, starting 5 minutes before creation time (for clock skew)</param>
        /// <param name="permissions">Bit mask based on <see cref="BlobContainerSasPermissions"/></param>
        /// <param name="blobName">Include to create a SAS for blob; when null, a container SAS will be created.</param>
        /// <returns>A resource URL with given <paramref name="permissions"/> enabled</returns>
        public static bool TryGetServiceSas(ILogger log, out string sas, string containerName, string uri, string accountName, 
            string accountKey, double hours = .2, int permissions = 1, string blobName = null)
        {
            sas = null;
            if (log.NullWarning(new { containerName, uri, accountName, accountKey }))
                return false;

            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                Resource = "c",
            };
            
            if (blobName is null) // SAS for container
            { 
                sasBuilder.SetPermissions((BlobContainerSasPermissions)permissions);
            } else // SAS for blob
            {
                sasBuilder.BlobName = blobName;
                sasBuilder.SetPermissions((BlobSasPermissions)permissions);
            }

            sasBuilder.StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5); // Account for clock skew
            sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(hours);
            
            string sasToken;
            try
            {
                var key = new StorageSharedKeyCredential(accountName, accountKey);
                sasToken = sasBuilder.ToSasQueryParameters(key).ToString();
            } catch(Exception e)
            {
                log.LogError(e, "Failed to generate SAS token.");
                return false;
            }

            sas = uri + sasToken;
            log.LogTrace("SAS for blob container {0} is: {1} (permissions are {2})", containerName, sas, (BlobContainerSasPermissions)permissions);

            return true;
        }
    }
}
