namespace Ignite.ExpertFinder.Dashboard.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Ignite.ExpertFinder.Contract;

    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    public class Communication
    {
        private static readonly Uri DetectionServiceUri =
            new Uri(FabricRuntime.GetActivationContext().ApplicationName + "/Detection");

        private readonly IExpertFinderOperations detectionServiceClient =
            ServiceProxy.Create<IExpertFinderOperations>(DetectionServiceUri, new ServicePartitionKey("basic"));

        public async Task AddExpertProfile(Expert expert)
        {
            await this.detectionServiceClient.AddExpertProfile(expert);
        }

        public async Task<string> ValidateImage(string imageUri)
        {
            return await this.detectionServiceClient.ValidateImage(imageUri);
        }

        public async Task<Verdict> DetectExperts(string imageUri)
        {
            var verdict = new Verdict();
            try
            {
                var experts = await this.detectionServiceClient.DetectExperts(imageUri);
                var verdictExperts = experts as IList<Expert> ?? experts.ToList();
                foreach (var expert in verdictExperts)
                {
                    var webClient = new WebClient();
                    expert.ProfilePicBase64Encoded = Convert.ToBase64String(webClient.DownloadData(expert.ProfilePicBlobUrl));
                }

                verdict.IsFaceDetected = verdictExperts.Any();
                verdict.Experts = verdictExperts;

                // save the result in dictionary so that it can be displayed on dashboard.
                var faceDetectionResponse = new FaceDetectionResponse
                {
                    DetectedExperts = verdictExperts,
                    CapturedImage = new Uri(imageUri),
                    ResponseTime = DateTime.UtcNow
                };
                await this.detectionServiceClient.SaveResponse(faceDetectionResponse);
                return verdict;
            }
            catch (Exception e)
            {
                verdict.Message = e.ToString();
                return verdict;
            }
        }

        public async Task<FaceDetectionResponse> GetDashboardReport()
        {
            return await this.detectionServiceClient.GetLastSavedResponse();
        }
    }
}