using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ignite.ExpertFinder.Contract
{
    public class FaceDetectionResponse
    {
        public IList<Expert> DetectedExperts { get; set; }
        public DateTime ResponseTime { get; set; }
        public Uri CapturedImage { get; set; }
    }
}
