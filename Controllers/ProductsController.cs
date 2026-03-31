using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductsService _productsService;

        public ProductsController(ProductsService productsService)
        {
            _productsService = productsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var result = await _productsService.GetProductsPageAsync();

            if (result == null)
                return NotFound(new { message = "No se encontró información de Products." });

            return Ok(result);
        }

    [Authorize]
    [HttpPut("page/{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdatePage(int id, [FromForm] ProductsPage request)
    {
        var updated = await _productsService.UpdateProductsPageAsync(id, request, Request.Scheme, Request.Host.ToString());

        if (!updated)
            return NotFound(new { message = "No se encontró la página de productos para actualizar." });

        return Ok(new { message = "Página de productos actualizada correctamente." });
    }

    [Authorize]
    [HttpPost("page")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreatePage([FromForm] ProductsPage request)
    {
        var newId = await _productsService.CreateProductsPageAsync(request, Request.Scheme, Request.Host.ToString());

        return Ok(new
        {
            message = "Página de productos creada correctamente.",
            id = newId
        });
    }

        [Authorize]
        [HttpPost("help-card")]
        public async Task<IActionResult> CreateHelpCard([FromBody] ProductHelpCard request)
        {
            var newId = await _productsService.CreateHelpCardAsync(request);

            return Ok(new
            {
                message = "Help card creada correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("help-card/{id}")]
        public async Task<IActionResult> UpdateHelpCard(int id, [FromBody] ProductHelpCard request)
        {
            var updated = await _productsService.UpdateHelpCardAsync(id, request);

            if (!updated)
                return NotFound(new { message = "No se encontró la help card para actualizar." });

            return Ok(new { message = "Help card actualizada correctamente." });
        }

        [Authorize]
        [HttpDelete("help-card/{id}")]
        public async Task<IActionResult> DeleteHelpCard(int id)
        {
            var deleted = await _productsService.DeleteHelpCardAsync(id);

            if (!deleted)
                return NotFound(new { message = "No se encontró la help card para eliminar." });

            return Ok(new { message = "Help card eliminada correctamente." });
        }

        [Authorize]
        [HttpPost("office")]
        public async Task<IActionResult> CreateOffice([FromBody] ProductOffice request)
        {
            var newId = await _productsService.CreateOfficeAsync(request);

            return Ok(new
            {
                message = "Oficina creada correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("office/{id}")]
        public async Task<IActionResult> UpdateOffice(int id, [FromBody] ProductOffice request)
        {
            var updated = await _productsService.UpdateOfficeAsync(id, request);

            if (!updated)
                return NotFound(new { message = "No se encontró la oficina para actualizar." });

            return Ok(new { message = "Oficina actualizada correctamente." });
        }

        [Authorize]
        [HttpDelete("office/{id}")]
        public async Task<IActionResult> DeleteOffice(int id)
        {
            var deleted = await _productsService.DeleteOfficeAsync(id);

            if (!deleted)
                return NotFound(new { message = "No se encontró la oficina para eliminar." });

            return Ok(new { message = "Oficina eliminada correctamente." });
        }
    }
}