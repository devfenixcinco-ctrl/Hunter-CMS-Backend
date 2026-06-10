using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Models;
using MreHunter.Api.Services;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/mission-accomplished")]
    [Tags("Misión Cumplida")]
        public class CompanyController : ControllerBase
    {
        private readonly CompanyService _companyService;

        public CompanyController(CompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompany()
        {
            var result = await _companyService.GetCompanyAsync();

            if (result == null)
                return NotFound(new { message = "No se encontró información de Company." });

            return Ok(result);
        }

        [Authorize]
        [HttpPost("page")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePage([FromForm] CompanyPage request)
        {
            var newId = await _companyService.CreateCompanyPageAsync(request, Request.Scheme, Request.Host.ToString());

            return Ok(new
            {
                message = "Página de empresa creada correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("page/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePage(int id, [FromForm] CompanyPage request)
        {
            var updated = await _companyService.UpdateCompanyPageAsync(id, request, Request.Scheme, Request.Host.ToString());

            if (!updated)
                return NotFound(new { message = "No se encontró la página de empresa para actualizar." });

            return Ok(new { message = "Página de empresa actualizada correctamente." });
        }

        [Authorize]
        [HttpPost("stats")]
        public async Task<IActionResult> CreateStats([FromBody] CompanyStat request)
        {
            var newId = await _companyService.CreateCompanyStatsAsync(request);

            return Ok(new
            {
                message = "Estadísticas creadas correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("stats")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateStats([FromBody] CompanyStat request)
        {   
            var updated = await _companyService.UpdateCompanyStatsAsync(request.Id, request);

            if (!updated)
                return NotFound(new { message = "No se encontraron estadísticas para actualizar." });

            return Ok(new { message = "Estadísticas actualizadas correctamente." });
        }
    }
}