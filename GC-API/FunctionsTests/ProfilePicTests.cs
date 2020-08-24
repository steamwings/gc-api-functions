using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Functions.Profile;
using FunctionsTests.Helpers;
using System.Runtime.CompilerServices;
using System.Text;
using Models.Database.User;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Azure.Documents;
using FunctionsTests.Extensions;

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
        private string _idBlockUrl;
        private GcUser _user;

        [TestInitialize]
        public void TestInit()
        {
            _container = TestHelper.GetStorageContainer(TestContext, TestHelper.StorageContainer.ProfilePics);
            _container.Create();
            TestHelper.SetupUserDb(TestContext);
            _token = TestHelper.Register(testUser);
            _user = TestHelper.GetOnlyUser(NullLogger.Instance);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHelper.Cleanup();
        }

        // [TestMethod] // This is normally run as part of other tests
        public void GetDefaultSasUrl()
        {
            var log = TestHelper.MakeLogger();
            var request = TestHelper.EmptyRequest;
            request.Headers.Add("Authorization", $"Bearer {_token}");

            var result = PictureUrl.Run(request, _container, log);

            Assert.That.IsOfType<OkObjectResult>(result, out var okResult);
            Assert.That.IsOfType<string>(okResult.Value, out var url);
            Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));
            _idBlockUrl = url.Trim('/') + '/' + _user.id;
        }

        // [TestMethod] // This is normally run as part of other tests
        public void GetUploadSasUrl()
        {
            var log = TestHelper.MakeLogger();
            var request = TestHelper.EmptyRequest;
            request.Headers.Add("Authorization", $"Bearer {_token}");
          
            var result = PictureUploadUrl.Run(request, _container, _user.id, log);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            Assert.That.IsOfType<OkObjectResult>(result, out var okResult);
            Assert.That.IsOfType<string>(okResult.Value, out var url);
            Assert.IsTrue(Uri.IsWellFormedUriString(url, UriKind.Absolute));
            _idBlockUrl = url;
        }

        [DataRow(nameof(GetDefaultSasUrl), false)]
        [DataRow(nameof(GetUploadSasUrl), true)]
        [DataTestMethod]
        public void Upload(string functionName, bool shouldPass)
        {
            typeof(ProfilePicTests).GetMethod(functionName).Invoke(this, new object[0]);

            // Upload picture 
            var cloudBlockBlob = new CloudBlockBlob(new Uri(_idBlockUrl));
            var uploadTask = cloudBlockBlob.UploadFromFileAsync(testPicPath);
            uploadTask.Wait();
            Assert.AreEqual(shouldPass, uploadTask.IsCompletedSuccessfully, $"Upload task had exception status {uploadTask.Status}, exception {uploadTask.Exception}");
        }

        [DataRow(nameof(GetDefaultSasUrl), true)]
        [DataRow(nameof(GetUploadSasUrl), false)]
        [DataTestMethod]
        public void Download(string functionName, bool shouldPass)
        {
            typeof(ProfilePicTests).GetMethod(functionName).Invoke(this, new object[0]);

            // Download picture 
            var cloudBlockBlob = new CloudBlockBlob(new Uri(_idBlockUrl));
            var downloadTask = cloudBlockBlob.DownloadTextAsync();
            downloadTask.Wait();
            Assert.AreEqual(shouldPass, downloadTask.IsCompletedSuccessfully, $"Upload task had exception status {downloadTask.Status}, exception {downloadTask.Exception}");
        }



    }
}
