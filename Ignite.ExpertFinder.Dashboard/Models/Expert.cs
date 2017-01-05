namespace Ignite.ExpertFinder.Dashboard.Models
{
    using System.Collections.Generic;

    public class Expert
    {
        public string name;
        public string organization;
        public string profilePicBlobURL;
        public List<Skills> skills;
    }
}