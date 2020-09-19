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
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
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

            typeof(ProfilePicTests).GetMethod(functionName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(this, new object[] { blob, shouldPass, nameof(GetDefaultSasUrl) });
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

            typeof(ProfilePicTests).GetMethod(functionName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(this, new object[] { blob, shouldPass, nameof(GetUploadSasUrl) });

        }

        private void Upload(CloudBlockBlob blob, bool shouldPass, [CallerMemberName] string caller = "")
        {
            //typeof(ProfilePicTests).GetMethod(functionName).Invoke(this, new object[0]);
            // Upload picture 
            //var cloudBlockBlob = new CloudBlockBlob(new Uri(_blockSas));

            Assert.That.ThrowsExceptionIf<StorageException>(!shouldPass, () => blob.UploadFromFile(testPicPath));
            
            //Assert.AreEqual(shouldPass, uploadTask.IsCompletedSuccessfully, XloadFailMessage(uploadTask, shouldPass, caller));
        }

        private void Download(CloudBlockBlob blob, bool shouldPass, [CallerMemberName] string caller = "")
        {
            //typeof(ProfilePicTests).GetMethod(functionName).Invoke(this, new object[0]);
            // Download picture 
            //var cloudBlockBlob = new CloudBlockBlob(new Uri(_blockSas));

            // Upload so there is something to download
            _container.GetBlockBlobReference(_user.id).UploadFromFile(testPicPath);

            Assert.That.ThrowsExceptionIf<StorageException>(!shouldPass, () => blob.DownloadText());

            //var downloadTask = blob.DownloadTextAsync();
            //downloadTask.Wait();
            //Assert.AreEqual(shouldPass, downloadTask.IsCompletedSuccessfully, XloadFailMessage(downloadTask, shouldPass, caller));
        }

        /// <summary>
        /// Generate an appropriate failure message in <see cref="Upload"/> or <see cref="Download"/>.
        /// </summary>
        /// <returns>A preformatted message.</returns>
        private string XloadFailMessage(Task task, bool shouldPass, string testName, [CallerMemberName] string caller = "")
         => $"{testName}: {caller} " + (shouldPass 
            ? $"had status {task.Status} and exception {task.Exception}" 
            : $"should not have succeeded!");
    }
}
