using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MreHunter.Api.Data;
using System.Text.Json;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomePageController : ControllerBase
    {
        private readonly Db _db;

        public HomePageController(Db db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var sections = new Dictionary<string, object?>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT section_name, content_json
                FROM home_sections;", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var sectionName = reader["section_name"]?.ToString() ?? string.Empty;
                var contentJson = reader["content_json"]?.ToString() ?? "{}";

                try
                {
                    var parsed = JsonSerializer.Deserialize<object>(contentJson);
                    sections[sectionName] = parsed;
                }
                catch
                {
                    sections[sectionName] = contentJson;
                }
            }

            return Ok(new
            {
                banner = sections.ContainsKey("banner_hunter_pets") ? sections["banner_hunter_pets"] : null,
                benefits = sections.ContainsKey("benefits_hunter_pets") ? sections["benefits_hunter_pets"] : null,
                creditSimulator = sections.ContainsKey("credit_simulator") ? sections["credit_simulator"] : null,
                historicalVideos = sections.ContainsKey("historical_videos") ? sections["historical_videos"] : null,
                mobileApp = sections.ContainsKey("mobile_app") ? sections["mobile_app"] : null,
                paymentMethods = sections.ContainsKey("payment_methods") ? sections["payment_methods"] : null
            });
        }
    }
}