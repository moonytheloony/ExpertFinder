namespace Ignite.ExpertFinder.Dashboard.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;

    using Ignite.ExpertFinder.Dashboard.Models;

    using Microsoft.AspNetCore.Mvc;

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
        public ActionResult DetectExperts(string imageUri)
        {
            var stubResponse = new Verdict();
            stubResponse.isFaceDetected = true;
            var experts = new List<Expert>();
            var skills = new List<Skills>();
            skills.Add(Skills.Architecture);
            skills.Add(Skills.Azure);
            skills.Add(Skills.HoloLens);
            skills.Add(Skills.Development);
            experts.Add(new Expert()
                {
                    name = "Rahul",
                    organization = "Readify",
                    profilePicBlobURL = "https://holoinput.blob.core.windows.net/registration/profilePicture.jpg",
                   skills = skills
                });

            stubResponse.experts = experts;
            return this.Ok(stubResponse);
        }

        [HttpPost]
        public ActionResult IngestImage(string imageUri)
        {
            return this.Ok();
        }
    }
}