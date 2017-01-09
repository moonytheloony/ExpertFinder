namespace Ignite.ExpertFinder.Dashboard.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    using Contract;
    using Utilities;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;
    using Microsoft.WindowsAzure.Storage;

    public class HomeController : Controller
    {
        private readonly IConfigurationRoot configuration;

        private readonly Communication communication;

        public HomeController(IConfigurationRoot configuration)
        {
            this.configuration = configuration;
            this.communication = new Communication();
        }

        public IActionResult Error()
        {
            return this.View();
        }

        public async Task<IActionResult> GetCapturedImage()
        {
            var storageAccount =
                CloudStorageAccount.Parse(this.configuration["ConnectionString:StorageConnectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("registration");
            var blockBlob = container.GetBlockBlobReference("log.txt");
            var lastBlob = await blockBlob.DownloadTextAsync();
            return this.Json(lastBlob);
        }

        public IActionResult Index()
        {
            this.ViewData["SubmissionStatus"] = this.TempData["SubmissionStatus"];
            var expert = new Expert();
            return this.View(expert);
        }

        public async Task<IActionResult> SaveImage()
        {
            using (var reader = new StreamReader(this.Request.Body))
            {
                var hexString = WebUtility.UrlEncode(reader.ReadToEnd());
                var imageName = Guid.NewGuid().ToString().ToLowerInvariant() + ".png";
                var storageAccount =
                    CloudStorageAccount.Parse(this.configuration["ConnectionString:StorageConnectionString"]);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("registration");
                var blockBlob = container.GetBlockBlobReference(imageName);
                var imageBytes = ConvertHexToBytes(hexString);
                await blockBlob.UploadFromByteArrayAsync(imageBytes, 0, imageBytes.Length);
                this.Request.Body.Dispose();
                var logBlob = container.GetBlockBlobReference("log.txt");
                await logBlob.UploadTextAsync(blockBlob?.Uri.ToString());
                return this.Content(blockBlob?.Uri.ToString());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Submit()
        {
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

           try
            {
                await this.communication.AddExpertProfile(expert);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            this.TempData["SubmissionStatus"] = "Details saved successfully.";
            return this.RedirectToAction("Index");
        }

        public async Task<IActionResult> ValidateImage()
        {
            StringValues sourceImageUri;
            return !this.Request.Form.TryGetValue("capturedImage", out sourceImageUri)
                ? this.Json("Faulty image. Please capture another picture.")
                : this.Json(await this.communication.ValidateImage(sourceImageUri));
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
    }
}