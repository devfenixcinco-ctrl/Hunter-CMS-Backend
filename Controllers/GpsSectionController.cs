using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/mission-accomplished/gps-section")]
    public class GpsSectionController : ControllerBase
    {
        private readonly GpsSectionService _gpsSectionService;

        public GpsSectionController(GpsSectionService gpsSectionService)
        {
            _gpsSectionService = gpsSectionService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var section = await _gpsSectionService.GetAsync();

            if (section == null)
                return NotFound(new { message = "No se encontró la sección GPS." });

            return Ok(section);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] GpsSection request)
        {
            var updated = await _gpsSectionService.UpdateAsync(request);

            if (!updated)
                return NotFound(new { message = "No se encontró la sección GPS para actualizar." });

            return Ok(new { message = "Sección GPS actualizada correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _gpsSectionService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "No se encontró la sección GPS para eliminar." });

            return Ok(new { message = "Sección GPS eliminada correctamente." });
        }
    }
}