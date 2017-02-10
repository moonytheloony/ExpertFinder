using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ignite.ExpertFinder.Contract
{
    using System.Threading.Tasks;

    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IExpertFinderOperations : IService
    {
        Task AddExpertProfile(Expert expert);

        Task<IEnumerable<Expert>> DetectExperts(string imageUri);

        Task<string> ValidateImage(string imageUri);

        Task SaveResponse(FaceDetectionResponse faceDetectionResponse);

        Task<FaceDetectionResponse> GetLastSavedResponse();

        Task ClearList();
    }
}
