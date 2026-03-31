using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MreHunter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
   
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Acceso OK (token válido)" });
    }
}
