using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                success = true,
                service = "StatusController",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
