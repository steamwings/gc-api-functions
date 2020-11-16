using Functions.Helpers;
using Functions.NonHttp;
using FunctionsTests.Extensions;
using FunctionsTests.Helpers;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace FunctionsTests
{
    [TestClass]
    public class ProfilePictureUploadedTests
    {
        private const string SampleGuid = "0f316fb1-db52-4790-b311-fe348a7a0cbe";
        private const string SamplePicPath = "samplePic";
        private static CloudBlobContainer PicsUploadsContainer;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            PicsUploadsContainer = TestHelper.CreateStorageContainer(context, StorageContainer.ProfilePicsUploads);

            // Download sample pic
            using var http = new HttpClient();
            using var downloadStream = await http.GetStreamAsync("https://via.placeholder.com/600");
            using var fileStream = File.OpenWrite(SamplePicPath);
            downloadStream.CopyTo(fileStream);
        }

        [TestMethod]
        public void ScalesSamplePic()
        {
            var blob = PicsUploadsContainer.GetBlockBlobReference(SampleGuid);
            var bytes = File.ReadAllBytes(SamplePicPath);
            blob.UploadFromByteArray(bytes, 0, bytes.Length);
            using var largeStream = new MemoryStream();
            using var mediumStream = new MemoryStream();
            using var smallStream = new MemoryStream();

            ProfilePictureUploaded.Run(blob, largeStream, mediumStream, smallStream, SampleGuid, NullLogger.Instance);

            AssertSquarePng(largeStream, ConfigKeys.ProfileWidthLarge);
            AssertSquarePng(mediumStream, ConfigKeys.ProfileWidthMedium);
            AssertSquarePng(smallStream, ConfigKeys.ProfileWidthSmall);
            Assert.IsFalse(blob.Exists());
        }

        /// <summary>
        /// Integration test to verify that scaled images are created 
        /// and appropriately added when a profile picture is uploaded
        /// </summary>
        [DataRow("https://via.placeholder.com/600x50.png")]
        [DataRow("https://via.placeholder.com/600.gif")]
        [DataRow("https://via.placeholder.com/1000.jpg")]
        [DataTestMethod]
        public async Task CreatesScaledImages(string samplePicUrl)
        {
            var blob = PicsUploadsContainer.GetBlockBlobReference(SampleGuid);

            using var http = new HttpClient();
            using var downloadStream = await http.GetStreamAsync(samplePicUrl);
            Assert.IsFalse(downloadStream.ToString().Contains("too big", System.StringComparison.OrdinalIgnoreCase));
            await blob.UploadFromStreamAsync(downloadStream);
            Assert.IsTrue(blob.Exists());
            using var largeStream = new MemoryStream();
            using var mediumStream = new MemoryStream();
            using var smallStream = new MemoryStream();

            ProfilePictureUploaded.Run(blob, largeStream, mediumStream, smallStream, SampleGuid, NullLogger.Instance);

            AssertSquarePng(largeStream, ConfigKeys.ProfileWidthLarge);
            AssertSquarePng(mediumStream, ConfigKeys.ProfileWidthMedium);
            AssertSquarePng(smallStream, ConfigKeys.ProfileWidthSmall);
            Assert.IsFalse(blob.Exists());
        }

        [TestMethod]
        public void IgnoresInvalidFilename()
        {
            var badId = "10f316fb";
            var blob = PicsUploadsContainer.GetBlockBlobReference(badId);
            var bytes = File.ReadAllBytes(SamplePicPath);
            blob.UploadFromByteArray(bytes, 0, bytes.Length);
            Assert.IsTrue(blob.Exists());
            using var largeStream = new MemoryStream();
            using var mediumStream = new MemoryStream();
            using var smallStream = new MemoryStream();

            ProfilePictureUploaded.Run(blob, largeStream, mediumStream, smallStream, badId, NullLogger.Instance);

            Assert.That.StreamNotWritten(smallStream, mediumStream, largeStream);
            Assert.IsFalse(blob.Exists());
        }

        [TestMethod]
        public void DeletesTextFile()
        {
            var blob = PicsUploadsContainer.GetBlockBlobReference(SampleGuid);
            blob.UploadText("Sample text");
            using var largeStream = new MemoryStream();
            using var mediumStream = new MemoryStream();
            using var smallStream = new MemoryStream();

            ProfilePictureUploaded.Run(blob, largeStream, mediumStream, smallStream, SampleGuid, NullLogger.Instance);

            Assert.That.StreamNotWritten(smallStream, mediumStream, largeStream);
            Assert.IsFalse(blob.Exists());
        }

        [TestMethod]
        public void DeletesInvalidPicture()
        {
            var blob = PicsUploadsContainer.GetBlockBlobReference(SampleGuid);
            var bytes = File.ReadAllBytes(SamplePicPath);
            bytes[1] = bytes[3] = 0xff; // Corrupt a little data
            blob.UploadFromByteArray(bytes, 0, bytes.Length);
            using var largeStream = new MemoryStream();
            using var mediumStream = new MemoryStream();
            using var smallStream = new MemoryStream();

            ProfilePictureUploaded.Run(blob, largeStream, mediumStream, smallStream, SampleGuid, NullLogger.Instance);

            Assert.That.StreamNotWritten(smallStream, mediumStream, largeStream);
            Assert.IsFalse(blob.Exists());
        }

        private void AssertSquarePng(Stream stream, ConfigKeys width)
        {
            Assert.IsTrue(0 < stream.Position);
            stream.Position = 0;
            AssertIsSquare(Image.Load<Rgba32>(stream, out var format).Bounds(), Config.Get(width, -1));
            Assert.IsInstanceOfType(format, typeof(PngFormat));
        }

        private void AssertIsSquare(Rectangle rectangle, double sideLength = 0)
        {
            if (rectangle.Height != rectangle.Width || (sideLength != 0 && rectangle.Width != sideLength))
                throw new AssertFailedException($"Rectangle had (h,w)=({rectangle.Height},{rectangle.Width}) but expected square"
                    + (sideLength != 0 ? "of size {sideLength}." : "."));
        }
    }
}
