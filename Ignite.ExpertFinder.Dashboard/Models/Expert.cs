namespace Ignite.ExpertFinder.Dashboard.Models
{
    using System.Collections.Generic;

    public class Expert
    {
        public string Name;
        public string Organization;
        public string ProfilePicBlobUrl;
        public List<Skills> Skills;
    }
}