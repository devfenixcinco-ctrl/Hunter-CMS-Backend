using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsPortableController : ControllerBase
    {
        private readonly ProductsPortableService _productsPortableService;

        public ProductsPortableController(ProductsPortableService productsPortableService)
        {
            _productsPortableService = productsPortableService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _productsPortableService.GetAsync();

            if (result == null)
                return NotFound(new { message = "No se encontró información de Products Portable." });

            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ProductsPortable request)
        {
            var newId = await _productsPortableService.CreateAsync(
                request,
                Request.Scheme,
                Request.Host.ToString()
            );

            return Ok(new
            {
                message = "Products Portable creado correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] ProductsPortable request)
        {
            var updated = await _productsPortableService.UpdateAsync(
                id,
                request,
                Request.Scheme,
                Request.Host.ToString()
            );

            if (!updated)
                return NotFound(new { message = "No se encontró Products Portable para actualizar." });

            return Ok(new { message = "Products Portable actualizado correctamente." });
        }
    }
}