using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/mission-accomplished/plan-plus-benefits")]
    public class PlanPlusBenefitController : ControllerBase
    {
        private readonly PlanPlusBenefitService _service;

        public PlanPlusBenefitController(PlanPlusBenefitService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var benefits = await _service.GetAllAsync();
            return Ok(benefits);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlanPlusBenefit request)
        {
            var id = await _service.CreateAsync(request);

            return Ok(new
            {
                message = "Benefit creado correctamente.",
                id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PlanPlusBenefit request)
        {
            var updated = await _service.UpdateAsync(id, request);

            if (!updated)
                return NotFound(new { message = "Benefit no encontrado." });

            return Ok(new { message = "Benefit actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "Benefit no encontrado." });

            return Ok(new { message = "Benefit eliminado correctamente." });
        }
    }
}