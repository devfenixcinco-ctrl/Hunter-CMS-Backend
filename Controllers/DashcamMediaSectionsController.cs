using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/dashcam-media-sections")]
    public class DashcamMediaSectionsController : ControllerBase
    {
        private readonly DashcamMediaSectionsService _service;

        public DashcamMediaSectionsController(DashcamMediaSectionsService service)
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
                return NotFound(new { message = "Media section no encontrada" });

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DashcamMediaSectionRequest req)
        {
            var id = await _service.CreateAsync(req);
            return Ok(new { message = "Media section creada correctamente", id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DashcamMediaSectionRequest req)
        {
            var updated = await _service.UpdateAsync(id, req);
            if (!updated)
                return NotFound(new { message = "Media section no encontrada" });

            return Ok(new { message = "Media section actualizada correctamente" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Media section no encontrada" });

            return Ok(new { message = "Media section eliminada correctamente" });
        }
    }
}