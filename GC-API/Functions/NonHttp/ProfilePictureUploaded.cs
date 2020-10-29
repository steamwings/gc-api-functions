using System;
using System.IO;
using Functions.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Functions.NonHttp
{
    public static class ProfilePictureUploaded
    {
        /// <summary>
        /// Respond to an uploaded profile picture by saving in standard format and sizes or deleting it
        /// </summary>
        /// <param name="profilePic"></param>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <remarks>
        /// TODO: Immediately revoke SAS Url used (probably requires Active Directory)
        /// </remarks>
        [FunctionName(nameof(ProfilePictureUploaded))]
        public static void Run([BlobTrigger("profile-pics-uploads/{name}", Connection = "SharedUserStorage")] CloudBlockBlob profilePic,
            [Blob("profile-pics/{name}-lg", FileAccess.Write, Connection = "SharedUserStorage")] Stream large,
            [Blob("profile-pics/{name}-md", FileAccess.Write, Connection = "SharedUserStorage")] Stream medium,
            [Blob("profile-pics/{name}-sm", FileAccess.Write, Connection = "SharedUserStorage")] Stream small,
            string name, ILogger log)
        {
            log.LogInformation($"${nameof(ProfilePictureUploaded)} processing blob (Name:{name} \n Size: {profilePic.Properties.Length} Bytes)");

            if (!Guid.TryParse(name, out _))
            {
                log.LogWarning("{0}: Filename '{1}' is not a guid.", nameof(ProfilePictureUploaded), name);
                goto end;
            }

            var encoder = new PngEncoder(); // Set this to whatever we want the default format to be
            int widthLarge = Config.Get(ConfigKeys.ProfileWidthLarge, 800);
            int widthMedium = Config.Get(ConfigKeys.ProfileWidthMedium, 300);
            int widthSmall = Config.Get(ConfigKeys.ProfileWidthSmall, 100);

            using (var blobStream = profilePic.OpenRead())
            {
                try
                {
                    using var input = Image.Load<Rgba32>(blobStream, out IImageFormat format);
                    log.LogTrace($"${nameof(ProfilePictureUploaded)} processing blob (Name:{name}, Format:{format.Name}");

                    var sideLength = Math.Min(input.Width, input.Height);
                    var centerCrop = new Rectangle(
                        input.Width > input.Height ? (input.Width - sideLength) / 2 : 0,
                        input.Height > input.Width ? (input.Height - sideLength) / 2 : 0,
                        sideLength, sideLength);
                    input.Mutate(x => CropExtensions.Crop(x, centerCrop));

                    input.Mutate(x => x.Resize(widthLarge, widthLarge));
                    input.Save(large, encoder);
                    input.Mutate(x => x.Resize(widthMedium, widthMedium));
                    input.Save(medium, encoder);
                    input.Mutate(x => x.Resize(widthSmall, widthSmall));
                    input.Save(small, encoder);
                }
                catch (Exception e) when (e is UnknownImageFormatException || e is InvalidImageContentException)
                {
                    // TODO Notify user the upload was invalid
                    log.LogWarning(e, $"{nameof(ProfilePictureUploaded)} read invalid profile picture '{name}'. The image will be deleted.");
                }
            }

        // Always delete once we're done
        end:
            profilePic.Delete();
        }
    }
}
