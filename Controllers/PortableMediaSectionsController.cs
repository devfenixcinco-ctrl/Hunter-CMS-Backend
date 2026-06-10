using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/portable-media-sections")]
    [Tags("Portable Media Sections")]
    public class PortableMediaSectionsController : ControllerBase
    {
        private readonly PortableMediaSectionsService _service;

        public PortableMediaSectionsController(PortableMediaSectionsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);

            if (item == null)
                return NotFound(new { message = "Media section no encontrada." });

            return Ok(item);
        }

        [Authorize]
        [HttpPost]
       
        public async Task<IActionResult> Create([FromBody] PortableMediaSectionRequest req)
        {
            var id = await _service.CreateAsync(req, Request.Scheme, Request.Host.ToString());
            return Ok(new { message = "Media section creada correctamente.", id });
        }

        [Authorize]
        [HttpPut("{id}")]
        
    
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "Media section no encontrada." });

            return Ok(new { message = "Media section eliminada correctamente." });
        }
    }
}