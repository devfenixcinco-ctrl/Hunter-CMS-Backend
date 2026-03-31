using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/newsletter")]
    public class NewsletterController : ControllerBase
    {
        private readonly Db _db;

        public NewsletterController(Db db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe([FromBody] NewsletterRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO newsletter (email)
                                 VALUES (@email);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", req.Email!.Trim());

            await cmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Email registrado correctamente" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT id, email FROM newsletter ORDER BY id DESC;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<object>();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    id = reader.GetInt32("id"),
                    email = reader.GetString("email")
                });
            }

            return Ok(list);
        }
    }
}