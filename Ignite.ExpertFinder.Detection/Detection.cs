namespace Ignite.ExpertFinder.Detection
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Ignite.ExpertFinder.Contract;

    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    using Newtonsoft.Json;

    internal sealed class Detection : StatefulService, IExpertFinderOperations
    {
        private const string UserProfileDictionary = "expertprofiles";
        private const string ResponseDictionary = "responsedictionary";
        private const string TrainingRequestsQueue = "trainingrequests";

        private readonly StatefulServiceContext context;

        private readonly FaceDetection faceDetection;

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
                // Find if this person exists.
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var userProfileDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(
                            UserProfileDictionary);
                    var existingUser = await userProfileDictionary.TryGetValueAsync(tx, expert.Email);
                    if (existingUser.HasValue)
                    {
                        var personId = Guid.Parse(JsonConvert.DeserializeObject<Expert>(existingUser.Value).Id);
                        await this.faceDetection.AddPersonToGroup(null, expert.ProfilePicBlobUrl, personId);
                        await this.faceDetection.TrainGroupAsync();
                        //var trainingRequestsQueue =
                        //    await this.StateManager.GetOrAddAsync<IReliableQueue<bool>>(TrainingRequestsQueue);
                        //await trainingRequestsQueue.EnqueueAsync(tx, true);
                    }

                    await tx.CommitAsync();
                    if (existingUser.HasValue)
                    {
                        return;
                    }
                }

                var registeredUserId = await this.faceDetection.AddPersonToGroup(expert.Email, expert.ProfilePicBlobUrl);
                using (var tx = this.StateManager.CreateTransaction())
                {
                    expert.Id = registeredUserId.ToString();
                    var userProfileDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(
                            UserProfileDictionary);
                    await userProfileDictionary.TryAddAsync(tx, expert.Email, JsonConvert.SerializeObject(expert));
                    await this.faceDetection.TrainGroupAsync();
                    //var trainingRequestsQueue =
                    //   await this.StateManager.GetOrAddAsync<IReliableQueue<bool>>(TrainingRequestsQueue);
                    //await trainingRequestsQueue.EnqueueAsync(tx, true);
                    await tx.CommitAsync();
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.Message(e.ToString());
            }
        }

        public async Task<IEnumerable<Expert>> DetectExperts(string imageUri)
        {
            var experts = new List<Expert>();
            var userIdList = await this.faceDetection.DetectFacesInPicture($"{imageUri}?salt={Guid.NewGuid()}");
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

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(this.CreateServiceRemotingListener) };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.faceDetection.Initialize;
                //await Task.Factory.StartNew(this.ProcessTrainingRequests, cancellationToken);
                cancellationToken.WaitHandle.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        private async Task ProcessTrainingRequests()
        {
            var trainingRequestsQueue = await this.StateManager.GetOrAddAsync<IReliableQueue<bool>>(TrainingRequestsQueue);
            while (true)
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var message = trainingRequestsQueue.TryDequeueAsync(tx).Result;
                    if (message.HasValue)
                    {
                        this.faceDetection.TrainGroup();
                    }

                    await tx.CommitAsync();
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        public async Task<string> ValidateImage(string imageUri)
        {
            try
            {
                return await this.faceDetection.IsImageUsable(imageUri) ? "One face detected. Please proceed with registration." : "Unusable image. Please clear the area, stand straight and capture another picture.";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public async Task SaveResponse(FaceDetectionResponse faceDetectionResponse)
        {
            using (var tx = this.StateManager.CreateTransaction())
            {
                var serializedString = JsonConvert.SerializeObject(faceDetectionResponse);
                var responseDictionary =
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(
                        ResponseDictionary);
                await responseDictionary.AddOrUpdateAsync(tx, "savedResponse", serializedString, (key, value) => serializedString);
                await tx.CommitAsync();
            }
        }

        public async Task<FaceDetectionResponse> GetLastSavedResponse()
        {
            var savedResponse = new FaceDetectionResponse { DetectedExperts = new List<Expert>() };
            using (var tx = this.StateManager.CreateTransaction())
            {
                var responseDictionary =
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(
                        ResponseDictionary);
                var response = await responseDictionary.TryGetValueAsync(tx, "savedResponse");
                if (response.HasValue)
                {
                    savedResponse = JsonConvert.DeserializeObject<FaceDetectionResponse>(response.Value);
                }

                await tx.CommitAsync();
            }

            return savedResponse;
        }

        public async Task ClearList()
        {
            await this.faceDetection.ClearGroup();
        }
    }
}