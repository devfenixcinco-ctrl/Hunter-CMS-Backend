using Microsoft.AspNetCore.Mvc;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/testdebug")]
    public class TestDebugController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("testdebug ok");
        }
    }
}