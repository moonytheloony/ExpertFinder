namespace Ignite.ExpertFinder.Detection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.ProjectOxford.Face;
    using Microsoft.ProjectOxford.Face.Contract;

    public class FaceDetection
    {
        private readonly FaceServiceClient faceServiceClient;

        private readonly string groupId;

        private readonly string groupName;

        public FaceDetection(string apiKey, string groupId, string groupName)
        {
            this.groupId = groupId;
            this.groupName = groupName;
            this.faceServiceClient = new FaceServiceClient(apiKey);
            this.Initialize = this.InitializeAsync();
        }

        public Task Initialize { get; private set; }

        public async void AddPersonToGroup(string personId, string profilePictureUri)
        {
            var person = await this.faceServiceClient.CreatePersonAsync(this.groupId, personId);
            await this.faceServiceClient.AddPersonFaceAsync(this.groupId, person.PersonId, profilePictureUri, personId);
        }

        public async Task<bool> IsImageUsable(string pictureUri)
        {
            var faces = await this.faceServiceClient.DetectAsync(pictureUri);
            return faces.Length == 1;
        }

        public async Task<IList<string>> DetectFacesInPicture(string pictureUri)
        {
            var identifiedPeopleIds = new List<string>();
            var faces = await this.faceServiceClient.DetectAsync(pictureUri);
            var faceIds = faces.Select(face => face.FaceId).ToArray();
            var results = await this.faceServiceClient.IdentifyAsync(this.groupId, faceIds);
            foreach (var identifyResult in results)
            {
                if (identifyResult.Candidates.Length == 0)
                {
                    continue;
                }

                var candidateId = identifyResult.Candidates[0].PersonId;
                var person = await this.faceServiceClient.GetPersonAsync(this.groupId, candidateId);
                identifiedPeopleIds.Add(person.Name);
            }

            return identifiedPeopleIds;
        }

        public async void TrainGroup()
        {
            await this.faceServiceClient.TrainPersonGroupAsync(this.groupId);
            while (true)
            {
                var trainingStatus = await this.faceServiceClient.GetPersonGroupTrainingStatusAsync(this.groupId);
                if (trainingStatus.Status != Status.Running)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private async Task InitializeAsync()
        {
            var expertGroup = await this.faceServiceClient.GetPersonGroupAsync(this.groupId);
            if (null == expertGroup)
            {
                await this.faceServiceClient.CreatePersonGroupAsync(this.groupId, this.groupName);
            }
        }
    }
}