namespace Ignite.ExpertFinder.Contract
{
    using System.Collections.Generic;

    public class Expert
    {
        public string Id;
        public string Email;
        public string Name;
        public string Organization;
        public string ProfilePicBlobUrl;
        public string ProfilePicBase64Encoded;
        public List<Skills> Skills;
    }
}