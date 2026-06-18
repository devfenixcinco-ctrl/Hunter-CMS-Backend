using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/header")]
    public class HeaderController : ControllerBase
    {
        private readonly Db _db;

        public HeaderController(Db db)
        {
            _db = db;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
            => reader[column] == DBNull.Value ? null : reader[column].ToString();

        private static bool IsInvalidString(string? value)
            => string.IsNullOrWhiteSpace(value);

        private static object NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private async Task<int?> GetLatestHeaderId(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand("SELECT id FROM header ORDER BY id DESC LIMIT 1;", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT * FROM header ORDER BY id DESC LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay header aún" });

            return Ok(new
            {
                id = reader.GetInt32("id"),
                supportLine = ReadNullableString(reader, "support_line"),
                theftNumber = ReadNullableString(reader, "theft_number"),
                lunes = ReadNullableString(reader, "lunes"),
                martes = ReadNullableString(reader, "martes"),
                miercoles = ReadNullableString(reader, "miercoles"),
                jueves = ReadNullableString(reader, "jueves"),
                viernes = ReadNullableString(reader, "viernes"),
                createdAt = reader.GetDateTime("created_at")
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] HeaderUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.SupportLine) || IsInvalidString(req.TheftNumber))
            {
                return BadRequest(new
                {
                    message = "SupportLine y TheftNumber son obligatorios y no pueden estar vacíos"
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var existingId = await GetLatestHeaderId(conn);

            if (existingId.HasValue)
            {
                const string updateSql = @"UPDATE header
                                           SET support_line=@support_line,
                                               theft_number=@theft_number,
                                               lunes=@lunes,
                                               martes=@martes,
                                               miercoles=@miercoles,
                                               jueves=@jueves,
                                               viernes=@viernes
                                           WHERE id=@id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@support_line", NormalizeValue(req.SupportLine));
                cmd.Parameters.AddWithValue("@theft_number", NormalizeValue(req.TheftNumber));
                cmd.Parameters.AddWithValue("@lunes", NormalizeValue(req.Lunes));
                cmd.Parameters.AddWithValue("@martes", NormalizeValue(req.Martes));
                cmd.Parameters.AddWithValue("@miercoles", NormalizeValue(req.Miercoles));
                cmd.Parameters.AddWithValue("@jueves", NormalizeValue(req.Jueves));
                cmd.Parameters.AddWithValue("@viernes", NormalizeValue(req.Viernes));
                cmd.Parameters.AddWithValue("@id", existingId.Value);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return NotFound(new { message = "No se pudo actualizar el header" });

                return Ok(new
                {
                    message = "Header actualizado",
                    created = false,
                    id = existingId.Value
                });
            }

            const string insertSql = @"INSERT INTO header
                                       (
                                           support_line,
                                           theft_number,
                                           lunes,
                                           martes,
                                           miercoles,
                                           jueves,
                                           viernes
                                       )
                                       VALUES
                                       (
                                           @support_line,
                                           @theft_number,
                                           @lunes,
                                           @martes,
                                           @miercoles,
                                           @jueves,
                                           @viernes
                                       );
                                       SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@support_line", NormalizeValue(req.SupportLine));
            insertCmd.Parameters.AddWithValue("@theft_number", NormalizeValue(req.TheftNumber));
            insertCmd.Parameters.AddWithValue("@lunes", NormalizeValue(req.Lunes));
            insertCmd.Parameters.AddWithValue("@martes", NormalizeValue(req.Martes));
            insertCmd.Parameters.AddWithValue("@miercoles", NormalizeValue(req.Miercoles));
            insertCmd.Parameters.AddWithValue("@jueves", NormalizeValue(req.Jueves));
            insertCmd.Parameters.AddWithValue("@viernes", NormalizeValue(req.Viernes));

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Header creado",
                created = true,
                id = newId
            });
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] HeaderUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.SupportLine) || IsInvalidString(req.TheftNumber))
            {
                return BadRequest(new
                {
                    message = "SupportLine y TheftNumber son obligatorios y no pueden estar vacíos"
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestHeaderId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe header para actualizar" });

            const string sql = @"UPDATE header
                                 SET support_line=@support_line,
                                     theft_number=@theft_number,
                                     lunes=@lunes,
                                     martes=@martes,
                                     miercoles=@miercoles,
                                     jueves=@jueves,
                                     viernes=@viernes
                                 WHERE id=@id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@support_line", NormalizeValue(req.SupportLine));
            cmd.Parameters.AddWithValue("@theft_number", NormalizeValue(req.TheftNumber));
            cmd.Parameters.AddWithValue("@lunes", NormalizeValue(req.Lunes));
            cmd.Parameters.AddWithValue("@martes", NormalizeValue(req.Martes));
            cmd.Parameters.AddWithValue("@miercoles", NormalizeValue(req.Miercoles));
            cmd.Parameters.AddWithValue("@jueves", NormalizeValue(req.Jueves));
            cmd.Parameters.AddWithValue("@viernes", NormalizeValue(req.Viernes));
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo actualizar el header" });

            return Ok(new
            {
                message = "Header actualizado (PUT)",
                id = latestId.Value
            });
        }
    
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestHeaderId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe header para eliminar" });

            using var cmd = new MySqlCommand("DELETE FROM header WHERE id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo eliminar el header" });

            return Ok(new
            {
                message = "Header eliminado",
                deleted = true,
                id = latestId.Value
            });
        }
    }
}