using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/pets-products-benefits")]
    [Tags("Producto Pets Benefits")]
    public class ProductsPetsBenefitController : ControllerBase
    {
        private readonly ProductsPetsBenefitService _service;

        public ProductsPetsBenefitController(ProductsPetsBenefitService service)
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
        public async Task<IActionResult> Create([FromBody] ProductsPetsBenefit request)
        {
            var id = await _service.CreateAsync(request);

            return Ok(new
            {
                message = "Benefit Pets creado correctamente.",
                id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductsPetsBenefit request)
        {
            var updated = await _service.UpdateAsync(id, request);

            if (!updated)
                return NotFound(new { message = "Benefit Pets no encontrado." });

            return Ok(new { message = "Benefit Pets actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "Benefit Pets no encontrado." });

            return Ok(new { message = "Benefit Pets eliminado correctamente." });
        }
    }
}