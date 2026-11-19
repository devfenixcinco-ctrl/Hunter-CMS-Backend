using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    /// <summary>
    /// Datos comunes a todas las páginas del sitio en una sola llamada (y una sola ida a la base de datos).
    /// Reemplaza 4 peticiones del frontend: /api/header, /api/footer, /api/settings y /api/contact-header.
    /// Cada bloque tiene exactamente la misma forma que su endpoint individual; si no hay datos, viene en null.
    /// </summary>
    [ApiController]
    [Route("api/site")]
    [Tags("Sitio (datos comunes)")]
    public class SiteController : ControllerBase
    {
        private readonly Db _db;

        public SiteController(Db db) => _db = db;

        [HttpGet("layout")]
        public async Task<IActionResult> GetLayout()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = string.Join("\n",
                HeaderController.SelectLatestSql,
                FooterController.SelectLatestSql,
                SettingsController.SelectLatestSql,
                ContactInfoController.SelectLatestSql);

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var header = await reader.ReadAsync() ? HeaderController.Map(reader) : null;

            await reader.NextResultAsync();
            var footer = await reader.ReadAsync() ? FooterController.Map(reader) : null;

            await reader.NextResultAsync();
            var settings = await reader.ReadAsync() ? SettingsController.Map(reader) : null;

            await reader.NextResultAsync();
            var contactInfo = await reader.ReadAsync() ? ContactInfoController.Map(reader) : null;

            return Ok(new { header, footer, settings, contactInfo });
        }
    }
}
