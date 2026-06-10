using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/mission-accomplished/plan-plus-section")]
    public class PlanPlusSectionController : ControllerBase
    {
        private readonly PlanPlusSectionService _planPlusSectionService;

        public PlanPlusSectionController(PlanPlusSectionService planPlusSectionService)
        {
            _planPlusSectionService = planPlusSectionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sections = await _planPlusSectionService.GetAllAsync();
            return Ok(sections);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var section = await _planPlusSectionService.GetByIdAsync(id);

            if (section == null)
                return NotFound(new { message = "No se encontró la sección Plan Plus." });

            return Ok(section);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlanPlusSection request)
        {
            var id = await _planPlusSectionService.CreateAsync(request);

            return Ok(new
            {
                message = "Sección Plan Plus creada correctamente.",
                id = id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PlanPlusSection request)
        {
            var updated = await _planPlusSectionService.UpdateAsync(id, request);

            if (!updated)
                return NotFound(new { message = "No se encontró la sección Plan Plus para actualizar." });

            return Ok(new { message = "Sección Plan Plus actualizada correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _planPlusSectionService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "No se encontró la sección Plan Plus para eliminar." });

            return Ok(new { message = "Sección Plan Plus eliminada correctamente." });
        }
    }
}