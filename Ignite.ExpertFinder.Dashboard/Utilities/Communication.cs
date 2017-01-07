using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ignite.ExpertFinder.Dashboard.Utilities
{
    using System.Fabric;
    using System.Net.Http;
    using System.Threading;


    using Microsoft.ServiceFabric.Services.Client;

    using Newtonsoft.Json.Linq;

    public class Communication
    {
        private readonly Uri detectionServiceUri = new Uri(FabricRuntime.GetActivationContext().ApplicationName + "/Detection");

        public async Task AddExpertProfile(Expert expert)
        {
            try
            {
                var tokenSource = new CancellationTokenSource();
                var servicePartitionResolver = ServicePartitionResolver.GetDefault();
                var httpClient = new HttpClient();
                var partition =
                    await
                        servicePartitionResolver.ResolveAsync(
                            this.detectionServiceUri,
                            new ServicePartitionKey("basic"),
                            tokenSource.Token);
                var ep = partition.GetEndpoint();
                var addresses = JObject.Parse(ep.Address);
                var primaryReplicaAddress = (string)addresses["Endpoints"].First;
                var primaryReplicaUriBuilder = new UriBuilder(primaryReplicaAddress)
                {
                    Query = $"subject={subject}&operation=queue"
                };
                var result = await httpClient.GetStringAsync(primaryReplicaUriBuilder.Uri);
                this.ViewBag.SearchTerm = result;
                return this.View();
            }
            catch (Exception e)
            {
                throw;
            }
        }


    }
}
