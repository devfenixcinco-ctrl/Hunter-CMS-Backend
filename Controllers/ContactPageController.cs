using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MreHunter.Api.Data;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactPageController : ControllerBase
    {
        private readonly Db _db;

        public ContactPageController(Db db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT phone, whatsapp_text, whatsapp_link, email
                FROM contact_info
                ORDER BY id DESC
                LIMIT 1;", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay información de contacto registrada." });

            return Ok(new
            {
                phone = reader["phone"]?.ToString(),
                whatsappText = reader["whatsapp_text"]?.ToString(),
                whatsappLink = reader["whatsapp_link"]?.ToString(),
                email = reader["email"]?.ToString()
            });
        }
    }
}