namespace Ignite.ExpertFinder.Dashboard.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Ignite.ExpertFinder.Dashboard.Models;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ProjectOxford.Face;

    public class ExpertFinderController : Controller
    {
        private readonly Uri registrationServiceEndpoint = new Uri(FabricRuntime.GetActivationContext().ApplicationName + "/Registration");

        private readonly Uri detectionServiceEndpoint = new Uri(FabricRuntime.GetActivationContext().ApplicationName + "/Detection");

        [HttpPost]
        public ActionResult CreateProfile(string faceId)
        {
            return null;
        }

        [HttpGet]
        public async Task<ActionResult> DetectExperts(string imageUri)
        {
            var faceServiceClient = new FaceServiceClient("041b73258acc45c69ff27dc5bea5bc8a");
            var faces = await faceServiceClient.DetectAsync(imageUri);
            var faceIds = faces.Select(face => face.FaceId).ToArray();
            var results = await faceServiceClient.IdentifyAsync("ignitetest", faceIds);
            foreach (var identifyResult in results)
            {
                Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                if (identifyResult.Candidates.Length == 0)
                {
                    Console.WriteLine("No one identified");
                }
                else
                {
                    // Get top 1 among all candidates returned
                    var candidateId = identifyResult.Candidates[0].PersonId;
                    var person = await faceServiceClient.GetPersonAsync("ignitetest", candidateId);
                    Console.WriteLine("Identified as {0}", person.Name);
                }
            }



            var stubResponse = new Verdict();
            stubResponse.IsFaceDetected = true;
            var experts = new List<Expert>();
            var skills = new List<Skills>();
            skills.Add(Skills.Architecture);
            skills.Add(Skills.Azure);
            skills.Add(Skills.HoloLens);
            skills.Add(Skills.Development);
            experts.Add(new Expert()
            {
                Name = "Rahul",
                Organization = "Readify",
                ProfilePicBlobUrl = "https://holoinput.blob.core.windows.net/registration/profilePicture.jpg",
                Skills = skills
            });

            stubResponse.Experts = experts;
            return this.Ok(stubResponse);
        }

        [HttpPost]
        public ActionResult IngestImage(string imageUri)
        {
            return this.Ok();
        }
    }
}