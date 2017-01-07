using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ignite.ExpertFinder.Contract
{
    using System.Threading.Tasks;

    public interface IExpertFinderOperations
    {
        Task AddExpertProfile(Expert expert);

        Task<IEnumerable<Expert>> DetectExperts(string imageUri);

        TaskStatus CheckTrainingStatus();
    }
}
