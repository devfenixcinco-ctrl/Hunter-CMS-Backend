using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/conventional-principal-banner")]
    [Tags("Producto Convencional (Principal Banner)")]
    public class BenefitsHeroController : ControllerBase
    {
        private readonly Db _db;

        public BenefitsHeroController(Db db)
        {
            _db = db;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
            => reader[column] == DBNull.Value ? null : reader[column].ToString();

        private bool HasInvalidRequiredFields(BenefitsHeroRequest req)
        {
            return string.IsNullOrWhiteSpace(req.Text);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, text, benefits_title
                        FROM benefits_hero
                        LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay principal banner configurado." });

            var item = new BenefitsHero
            {
                Id = reader.GetInt32("id"),
                Text = reader.GetString("text"),
                BenefitsTitle = ReadNullableString(reader, "benefits_title")
            };

            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BenefitsHeroRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(request))
                return BadRequest(new
                {
                    message = "Text es obligatorio y no puede estar vacío."
                });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id FROM benefits_hero LIMIT 1;", conn);
            var existingIdObj = await getIdCmd.ExecuteScalarAsync();

            if (existingIdObj != null)
            {
                int id = Convert.ToInt32(existingIdObj);

                var updateSql = @"UPDATE benefits_hero
                                  SET text = @text,
                                      benefits_title = @benefits_title
                                  WHERE id = @id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@text", request.Text!.Trim());
                cmd.Parameters.AddWithValue("@benefits_title", (object?)request.BenefitsTitle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Principal banner actualizado",
                    created = false,
                    id
                });
            }

            var insertSql = @"INSERT INTO benefits_hero
                              (
                                  text,
                                  benefits_title
                              )
                              VALUES
                              (
                                  @text,
                                  @benefits_title
                              );
                              SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@text", request.Text!.Trim());
            insertCmd.Parameters.AddWithValue("@benefits_title", (object?)request.BenefitsTitle ?? DBNull.Value);

            var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Principal banner creado",
                created = true,
                id = newId
            });
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BenefitsHeroRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(request))
                return BadRequest(new
                {
                    message = "Text es obligatorio y no puede estar vacío."
                });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var checkCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM benefits_hero WHERE id = @id;", conn);

            checkCmd.Parameters.AddWithValue("@id", id);

            var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (exists == 0)
                return NotFound(new { message = "No existe principal banner para actualizar." });

            var sql = @"UPDATE benefits_hero
                        SET text = @text,
                            benefits_title = @benefits_title
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@text", request.Text!.Trim());
            cmd.Parameters.AddWithValue("@benefits_title", (object?)request.BenefitsTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                message = "Principal banner actualizado",
                id
            });
        }
    }
}