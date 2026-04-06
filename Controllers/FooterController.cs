using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/footer")]
    public class FooterController : ControllerBase
    {
        private readonly Db _db;

        public FooterController(Db db)
        {
            _db = db;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
            => reader[column] == DBNull.Value ? null : reader[column].ToString();

        private static bool IsInvalidString(string? value)
            => string.IsNullOrWhiteSpace(value);

        private static object NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private async Task<int?> GetLatestFooterId(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand("SELECT id FROM footer ORDER BY id DESC LIMIT 1;", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT * FROM footer ORDER BY id DESC LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay footer aún" });

            var data = new FooterInfo
            {
                Id = reader.GetInt32("id"),
                FooterText = ReadNullableString(reader, "footer_text"),
                Address = ReadNullableString(reader, "address"),
                Email = ReadNullableString(reader, "email"),
                SocialUsername = ReadNullableString(reader, "social_username"),
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(data);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] FooterUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.FooterText) ||
                IsInvalidString(req.Address) ||
                IsInvalidString(req.Email) ||
                IsInvalidString(req.SocialUsername))
            {
                return BadRequest(new
                {
                    message = "Todos los campos son obligatorios y no pueden estar vacíos"
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var existingId = await GetLatestFooterId(conn);

            if (existingId.HasValue)
            {
                const string updateSql = @"UPDATE footer
                                           SET footer_text=@text,
                                               address=@addr,
                                               email=@mail,
                                               social_username=@user
                                           WHERE id=@id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@text", NormalizeValue(req.FooterText));
                cmd.Parameters.AddWithValue("@addr", NormalizeValue(req.Address));
                cmd.Parameters.AddWithValue("@mail", NormalizeValue(req.Email));
                cmd.Parameters.AddWithValue("@user", NormalizeValue(req.SocialUsername));
                cmd.Parameters.AddWithValue("@id", existingId.Value);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return NotFound(new { message = "No se pudo actualizar el footer" });

                return Ok(new
                {
                    message = "Footer actualizado (POST)",
                    created = false,
                    id = existingId.Value
                });
            }

            const string insertSql = @"INSERT INTO footer (footer_text, address, email, social_username)
                                       VALUES (@text, @addr, @mail, @user);
                                       SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@text", NormalizeValue(req.FooterText));
            insertCmd.Parameters.AddWithValue("@addr", NormalizeValue(req.Address));
            insertCmd.Parameters.AddWithValue("@mail", NormalizeValue(req.Email));
            insertCmd.Parameters.AddWithValue("@user", NormalizeValue(req.SocialUsername));

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Footer creado (POST)",
                created = true,
                id = newId
            });
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] FooterUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.FooterText) ||
                IsInvalidString(req.Address) ||
                IsInvalidString(req.Email) ||
                IsInvalidString(req.SocialUsername))
            {
                return BadRequest(new
                {
                    message = "Todos los campos son obligatorios y no pueden estar vacíos"
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestFooterId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe footer para actualizar" });

            const string sql = @"UPDATE footer
                                 SET footer_text=@text,
                                     address=@addr,
                                     email=@mail,
                                     social_username=@user
                                 WHERE id=@id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@text", NormalizeValue(req.FooterText));
            cmd.Parameters.AddWithValue("@addr", NormalizeValue(req.Address));
            cmd.Parameters.AddWithValue("@mail", NormalizeValue(req.Email));
            cmd.Parameters.AddWithValue("@user", NormalizeValue(req.SocialUsername));
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo actualizar el footer" });

            return Ok(new
            {
                message = "Footer actualizado (PUT)",
                id = latestId.Value
            });
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestFooterId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe footer para eliminar" });

            using var cmd = new MySqlCommand("DELETE FROM footer WHERE id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo eliminar el footer" });

            return Ok(new
            {
                message = "Footer eliminado",
                deleted = true,
                id = latestId.Value
            });
        }
    }
}