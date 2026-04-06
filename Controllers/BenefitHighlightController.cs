using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/conventional-highlight")]
    [Tags("Producto Convencional (Highlight)    ")]
    public class BenefitHighlightController : ControllerBase
    {
        private readonly Db _db;

        public BenefitHighlightController(Db db)
        {
            _db = db;
        }

        private bool HasInvalidRequiredFields(BenefitHighlightRequest req)
        {
            return string.IsNullOrWhiteSpace(req.Title) ||
                   string.IsNullOrWhiteSpace(req.Description);
               
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, title, description, created_at
                        FROM benefit_highlight
                        LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay highlight configurado." });

            var item = new BenefitHighlight
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Description = reader["description"]?.ToString(),
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] BenefitHighlightRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(req))
            {
                return BadRequest(new
                {
                    message = "Title y Description son obligatorios y no pueden estar vacíos."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id FROM benefit_highlight LIMIT 1;", conn);
            var existingIdObj = await getIdCmd.ExecuteScalarAsync();

            if (existingIdObj != null)
            {
                int id = Convert.ToInt32(existingIdObj);

                var updateSql = @"UPDATE benefit_highlight
                                  SET title = @title,
                                      description = @description
                                  WHERE id = @id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
                cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Highlight actualizado",
                    created = false,
                    id
                });
            }

            var insertSql = @"INSERT INTO benefit_highlight
                              (title, description)
                              VALUES
                              (@title, @description);
                              SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            insertCmd.Parameters.AddWithValue("@description", req.Description?.Trim());


            var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Highlight creado",
                created = true,
                id = newId
            });
        }

        [Authorize]
        
        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromBody] BenefitHighlightRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(req))
            {
                return BadRequest(new
                {
                    message = "Title, Description, BackgroundColor y TextColor son obligatorios y no pueden estar vacíos."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id FROM benefit_highlight LIMIT 1;", conn);
            var existingIdObj = await getIdCmd.ExecuteScalarAsync();

            if (existingIdObj == null)
                return NotFound(new { message = "No existe highlight para actualizar." });

            int id = Convert.ToInt32(existingIdObj);

            var sql = @"UPDATE benefit_highlight
                        SET title = @title,
                            description = @description
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Highlight actualizado", id });
        }
    }
}