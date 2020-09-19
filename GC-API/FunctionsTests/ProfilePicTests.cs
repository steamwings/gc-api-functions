using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Functions.Profile;
using FunctionsTests.Helpers;
using Models.Database.User;
using Microsoft.Extensions.Logging.Abstractions;
using FunctionsTests.Extensions;
using System.Reflection;
using System.IO;
using Microsoft.Azure.Storage;

namespace FunctionsTests
{
    [TestClass]
    public class ProfilePicTests
    {
        private static readonly (string name, string email, string password) testUser = TestHelper.TestUsers[0];
        private static readonly string testPicPath = TestHelper.TestPictures[0];

        public TestContext TestContext { get; set; }

        private CloudBlobContainer _container;
        private string _token;
        private GcUser _user;

        [TestInitialize]
        public void TestInit()
        {
            _container = TestHelper.CreateStorageContainer(TestContext, TestHelper.StorageContainer.ProfilePics);
            TestHelper.SetupUserDb(TestContext);
            _token = TestHelper.Register(testUser);
            _user = TestHelper.GetOnlyUser(NullLogger.Instance);
            Assert.IsTrue(File.Exists(testPicPath));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHelper.Cleanup();
        }

        [DataRow(nameof(Download), true)]
        [DataRow(nameof(Upload), false)]
        [DataTestMethod]
        public void GetDefaultSasUrl(string functionName, bool shouldPass)
        {
            var log = TestHelper.MakeLogger();
            var request = TestHelper.EmptyRequest;
            request.Headers.Add("Authorization", $"Bearer {_token}");

            var result = PictureUrl.Run(request, _container, log);

            Assert.That.IsOfType<OkObjectResult>(result, out var okResult);
            Assert.That.IsOfType<string>(okResult.Value, out var url);
            Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));

            var container = new CloudBlobContainer(new Uri(url));
            Assert.AreEqual(_container.Uri.ToString(), container.Uri.ToString());

            var blob = container.GetBlockBlobReference(_user.id);

            Invoke(functionName, blob, shouldPass);
        }

        [DataRow(nameof(Upload), true)]
        [DataRow(nameof(Download), false)]
        [DataTestMethod]
        public void GetUploadSasUrl(string functionName, bool shouldPass)
        {
            var log = TestHelper.MakeLogger();
            var request = TestHelper.EmptyRequest;
            request.Headers.Add("Authorization", $"Bearer {_token}");

            var userPicBlob = _container.GetBlockBlobReference(_user.id);
            var result = PictureUploadUrl.Run(request, userPicBlob, _user.id, log);

            Assert.That.IsOfType<OkObjectResult>(result, out var okResult);
            Assert.That.IsOfType<string>(okResult.Value, out var url);
            Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));

            var blob = new CloudBlockBlob(new Uri(url));
            Assert.AreEqual(userPicBlob.Uri.ToString(), blob.Uri.ToString());

            Invoke(functionName, blob, shouldPass);
        }

        /// <summary>
        /// Helper method to simplify test invocations
        /// </summary>
        private void Invoke(string functionName, CloudBlockBlob blob, bool shouldPass)
        {
            typeof(ProfilePicTests).GetMethod(functionName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(this, new object[] { blob, shouldPass });
        }

        private void Upload(CloudBlockBlob blob, bool shouldPass)
        {
            Assert.That.ThrowsExceptionIf<StorageException>(!shouldPass, () => blob.UploadFromFile(testPicPath));
        }

        private void Download(CloudBlockBlob blob, bool shouldPass)
        {
            // Upload so there is something to download
            _container.GetBlockBlobReference(_user.id).UploadFromFile(testPicPath);

            Assert.That.ThrowsExceptionIf<StorageException>(!shouldPass, () => blob.DownloadText());
        }
    }
}
