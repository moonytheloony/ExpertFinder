using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Ignite.ExpertFinder.Dashboard.Controllers
{
    using System.IO;

    using Ignite.ExpertFinder.Dashboard.Models;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;
    using Microsoft.ProjectOxford.Face;
    using Microsoft.ProjectOxford.Face.Contract;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class HomeController : Controller
    {
        IConfigurationRoot _configuration;

        public HomeController(IConfigurationRoot configuration)
        {
            this._configuration = configuration;
        }

        public IActionResult Index()
        {
            this.ViewData["SubmissionStatus"] = this.TempData["SubmissionStatus"];
            var expert = new Expert();
            return this.View(expert);
        }

        [HttpPost]
        public async Task<IActionResult> Submit()
        {
            // Collect data.
            var expert = new Expert
            {
                Name = this.Request.Form["Name"].ToString(),
                Organization = this.Request.Form["Organization"].ToString(),
                ProfilePicBlobUrl = this.Request.Form["ProfilePicBlobUrl"].ToString(),
                Skills = new List<Skills>()
            };
            foreach (var skillString in this.Request.Form["Skills"].ToString().Split(','))
            {
                expert.Skills.Add((Skills)Enum.Parse(typeof(Skills), skillString, true));
            }

            //Add oerson to face list
            var faceServiceClient = new FaceServiceClient("041b73258acc45c69ff27dc5bea5bc8a");
            string personGroupId = "ignitetest";
            await faceServiceClient.CreatePersonGroupAsync(personGroupId, "ignitees");

            CreatePersonResult friend1 = await faceServiceClient.CreatePersonAsync(personGroupId,expert.Name);
            await faceServiceClient.AddPersonFaceAsync(personGroupId, friend1.PersonId, expert.ProfilePicBlobUrl, expert.Name);
            // In service this would be different. https://www.microsoft.com/cognitive-services/en-us/face-api/documentation/face-api-how-to-topics/howtoidentifyfacesinimage
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);


            this.TempData["SubmissionStatus"] = "Details saved successfully.";
            return this.RedirectToAction("Index");
        }

        public async Task<IActionResult> GetCapturedImage()
        {
            var storageAccount = CloudStorageAccount.Parse(this._configuration["ConnectionString:StorageConnectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("registration");
            var blockBlob = container.GetBlockBlobReference("log.txt");
            var lastBlob = await blockBlob.DownloadTextAsync();
            return this.Json(lastBlob);
        }

        private static byte[] ConvertHexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        public async Task<IActionResult> ValidateImage()
        {
            try
            {
                StringValues sourceImageUri;
                if (!this.Request.Form.TryGetValue("capturedImage", out sourceImageUri))
                {
                    return this.Json("Faulty image. Please capture another picture.");
                }

                var faceServiceClient = new FaceServiceClient("041b73258acc45c69ff27dc5bea5bc8a");
                var faces = await faceServiceClient.DetectAsync(sourceImageUri.ToString());
                return this.Json(faces.Length == 1 ? "One face detected. Please proceed with registration." : "Unusable image. Please clear the area, stand straight and capture another picture.");
            }
            catch (Exception ex)
            {
                return this.Json(ex.ToString());
            }
        }

        public async Task<IActionResult> SaveImage()
        {
            using (var reader = new StreamReader(this.Request.Body))
            {
                var hexString = System.Net.WebUtility.UrlEncode(reader.ReadToEnd());
                var imageName = Guid.NewGuid().ToString().ToLowerInvariant() + ".png";
                var storageAccount = CloudStorageAccount.Parse(this._configuration["ConnectionString:StorageConnectionString"]);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("registration");
                var blockBlob = container.GetBlockBlobReference(imageName);
                var imageBytes = ConvertHexToBytes(hexString);
                await blockBlob.UploadFromByteArrayAsync(imageBytes, 0, imageBytes.Length);
                this.Request.Body.Dispose();
                // Write the name of blob to text file.
                var logBlob = container.GetBlockBlobReference("log.txt");
                await logBlob.UploadTextAsync(blockBlob?.Uri.ToString());
                return this.Content(blockBlob?.Uri.ToString());
            }
        }

        public IActionResult Error()
        {
            return this.View();
        }
    }
}
