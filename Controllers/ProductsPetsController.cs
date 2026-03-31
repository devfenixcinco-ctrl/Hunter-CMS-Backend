using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsPetsController : ControllerBase
    {
        private readonly ProductsPetsService _productsPetsService;

        public ProductsPetsController(ProductsPetsService productsPetsService)
        {
            _productsPetsService = productsPetsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _productsPetsService.GetAsync();

            if (result == null)
                return NotFound(new { message = "No se encontró información de Products Pets." });

            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ProductsPets request)
        {
            var newId = await _productsPetsService.CreateAsync(
                request,
                Request.Scheme,
                Request.Host.ToString()
            );

            return Ok(new
            {
                message = "Products Pets creado correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] ProductsPets request)
        {
            var updated = await _productsPetsService.UpdateAsync(
                id,
                request,
                Request.Scheme,
                Request.Host.ToString()
            );

            if (!updated)
                return NotFound(new { message = "No se encontró Products Pets para actualizar." });

            return Ok(new { message = "Products Pets actualizado correctamente." });
        }
    }
}