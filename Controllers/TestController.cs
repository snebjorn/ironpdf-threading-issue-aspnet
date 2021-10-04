using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ironpdf_threading_issue_aspnet
{
    public class TestController : Controller
    {
        private readonly Worker worker;

        public TestController(Worker worker)
        {
            this.worker = worker;
        }

        [HttpGet]
        [Route("simple")]
        public async Task<IActionResult> GetSimple()
        {
            await worker.DoSimpleWorkAsync();
            return Ok();
        }

        [HttpGet]
        [Route("seq")]
        public async Task<IActionResult> GetSequential()
        {
            await worker.DoSequentialWorkAsync();
            return Ok();
        }

        [HttpGet]
        [Route("adv")]
        public async Task<IActionResult> GetAdvanced()
        {
            await worker.DoAdvancedWorkAsync();
            return Ok();
        }

        [HttpGet]
        [Route("table")]
        public async Task<IActionResult> GetTable()
        {
            await worker.DoTableBreakWorkAsync();
            return Ok();
        }
    }
}
