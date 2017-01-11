namespace Ignite.ExpertFinder.Contract
{
    using System.Collections.Generic;

    public class Verdict
    {
        public bool IsFaceDetected;
        public IEnumerable<Expert> Experts;
        public string Message { get; set; }
    }
}