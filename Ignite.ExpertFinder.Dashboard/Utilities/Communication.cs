namespace Ignite.ExpertFinder.Dashboard.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
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

        public async Task<IEnumerable<Expert>> DetectExperts(string imageUri)
        {
            return await this.detectionServiceClient.DetectExperts(imageUri);
        }
    }
}