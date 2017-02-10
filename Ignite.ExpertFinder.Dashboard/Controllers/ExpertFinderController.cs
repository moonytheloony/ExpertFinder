namespace Ignite.ExpertFinder.Dashboard.Controllers
{
    using System.Threading.Tasks;

    using Utilities;

    using Microsoft.AspNetCore.Mvc;

    public class ExpertFinderController : Controller
    {
        private readonly Communication communication;

        public ExpertFinderController()
        {
            this.communication = new Communication();
        }

        [HttpGet]
        public async Task<ActionResult> DetectExperts(string imageUri)
        {
            return this.Ok(new { isComplete = true, detectionVerdict = await this.communication.DetectExperts(imageUri) });
        }

        [HttpGet]
        public async Task<ActionResult> ClearList()
        {
            await this.communication.ClearList();
            return this.Ok();
        }
    }
}