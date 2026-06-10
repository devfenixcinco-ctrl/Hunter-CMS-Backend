using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/portable-benefits")]
    [Tags("Portable Benefits")]
    public class PortableBenefitsController : ControllerBase
    {
        private readonly PortableBenefitsService _service;

        public PortableBenefitsController(PortableBenefitsService service)
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
                return NotFound();

            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PortableBenefitRequest req)
        {
            var id = await _service.CreateAsync(req);
            return Ok(new { message = "Benefit creado", id });
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PortableBenefitRequest req)
        {
            var updated = await _service.UpdateAsync(id, req);

            if (!updated)
                return NotFound();

            return Ok(new { message = "Benefit actualizado" });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return Ok(new { message = "Benefit eliminado" });
        }
    }
}