using Microsoft.AspNetCore.Mvc;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { ok = true, message = "pong" });
        }
    }
}
