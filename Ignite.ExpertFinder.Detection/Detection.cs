namespace Ignite.ExpertFinder.Detection
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Ignite.ExpertFinder.Contract;

    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    using Newtonsoft.Json;

    internal sealed class Detection : StatefulService, IExpertFinderOperations
    {
        private const string UserProfileDictionary = "expertprofiles";

        private readonly StatefulServiceContext context;

        private readonly FaceDetection faceDetection;

        private Task trainingTask;

        public Detection(StatefulServiceContext context)
            : base(context)
        {
            this.context = context;
            var configurationPackage = this.context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var faceDetectionApiKey =
                configurationPackage.Settings.Sections["ApplicationSettings"].Parameters["FaceDetectionApiKey"].Value;
            var faceDetectionGroupId =
                configurationPackage.Settings.Sections["ApplicationSettings"].Parameters["FaceDetectionGroupId"].Value;
            var faceDetectionGroupName =
                configurationPackage.Settings.Sections["ApplicationSettings"].Parameters["FaceDetectionGroupName"].Value;
            this.faceDetection = new FaceDetection(faceDetectionApiKey, faceDetectionGroupId, faceDetectionGroupName);
        }

        public async Task AddExpertProfile(Expert expert)
        {
            try
            {
                expert.Id = Guid.NewGuid().ToString();
                this.faceDetection.AddPersonToGroup(expert.Id, expert.ProfilePicBlobUrl);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var userProfileDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(
                            UserProfileDictionary);
                    await userProfileDictionary.TryAddAsync(tx, expert.Id, JsonConvert.SerializeObject(expert));
                    await tx.CommitAsync();
                }

                // Launch training method. Return to caller.
                this.trainingTask = Task.Run(() => this.faceDetection.TrainGroup());
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message(e.ToString());
            }
        }

        public TaskStatus CheckTrainingStatus()
        {
            return this.trainingTask?.Status ?? TaskStatus.WaitingForActivation;
        }

        public async Task<IEnumerable<Expert>> DetectExperts(string imageUri)
        {
            try
            {
                var experts = new List<Expert>();
                var userIdList = await this.faceDetection.DetectFacesInPicture(imageUri);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var userProfileDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(
                            UserProfileDictionary);
                    foreach (var userId in userIdList)
                    {
                        var userProfileRecord = await userProfileDictionary.TryGetValueAsync(tx, userId);
                        if (userProfileRecord.HasValue)
                        {
                            experts.Add(JsonConvert.DeserializeObject<Expert>(userProfileRecord.Value));
                        }
                    }
                    await tx.CommitAsync();
                }

                return experts;
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message(e.ToString());
                return Enumerable.Empty<Expert>();
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await this.faceDetection.Initialize;
            cancellationToken.WaitHandle.WaitOne();
        }
    }
}