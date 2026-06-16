using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/pets-products-media-sections")]
    public class ProductsPetsMediaSectionController : ControllerBase
    {
        private readonly ProductsPetsMediaSectionService _service;

        public ProductsPetsMediaSectionController(ProductsPetsMediaSectionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductsPetsMediaSection item)
        {
            var result = await _service.CreateAsync(item);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductsPetsMediaSection item)
        {
            var updated = await _service.UpdateAsync(id, item);

            if (!updated)
            {
                return NotFound(new { message = "Media section not found" });
            }

            return Ok(new { message = "Media section updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new { message = "Media section not found" });
            }

            return Ok(new { message = "Media section deleted successfully" });
        }
    }
}