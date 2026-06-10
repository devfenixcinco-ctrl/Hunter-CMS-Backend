using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/dashcam-benefits")]
    public class DashcamBenefitsController : ControllerBase
    {
        private readonly DashcamBenefitsService _service;

        public DashcamBenefitsController(DashcamBenefitsService service)
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
                return NotFound(new { message = "Benefit no encontrado" });

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DashcamBenefitRequest req)
        {
            var id = await _service.CreateAsync(req);
            return Ok(new { message = "Benefit creado correctamente", id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DashcamBenefitRequest req)
        {
            var updated = await _service.UpdateAsync(id, req);
            if (!updated)
                return NotFound(new { message = "Benefit no encontrado" });

            return Ok(new { message = "Benefit actualizado correctamente" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Benefit no encontrado" });

            return Ok(new { message = "Benefit eliminado correctamente" });
        }
    }
}