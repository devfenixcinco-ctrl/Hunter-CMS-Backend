using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MreHunter.Api.Data;
using MreHunter.Api.Models;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeSectionController : ControllerBase
    {
        private readonly Db _db;

        public HomeSectionController(Db db)
        {
            _db = db;
        }

      
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = new List<object>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT id, section_name, content_json, created_at, updated_at
                FROM home_sections
                ORDER BY id ASC;", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    id = reader.GetInt32("id"),
                    sectionName = reader["section_name"]?.ToString(),
                    contentJson = reader["content_json"]?.ToString(),
                    createdAt = reader["created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["created_at"]),
                    updatedAt = reader["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["updated_at"])
                });
            }

            return Ok(result);
        }

        [HttpGet("{sectionName}")]
        public async Task<IActionResult> GetBySectionName(string sectionName)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT id, section_name, content_json, created_at, updated_at
                FROM home_sections
                WHERE section_name = @section_name
                LIMIT 1;", conn);

            cmd.Parameters.AddWithValue("@section_name", sectionName);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "Sección no encontrada." });

            return Ok(new
            {
                id = reader.GetInt32("id"),
                sectionName = reader["section_name"]?.ToString(),
                contentJson = reader["content_json"]?.ToString(),
                createdAt = reader["created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["created_at"]),
                updatedAt = reader["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["updated_at"])
            });
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HomeSectionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using (var check = new MySqlCommand(@"
                SELECT COUNT(*)
                FROM home_sections
                WHERE section_name = @section_name;", conn))
            {
                check.Parameters.AddWithValue("@section_name", request.SectionName);
                var count = Convert.ToInt64(await check.ExecuteScalarAsync());

                if (count > 0)
                    return Conflict(new { message = "Ya existe una sección con ese nombre. Usa PUT para actualizar." });
            }

            using var cmd = new MySqlCommand(@"
                INSERT INTO home_sections
                (
                    section_name,
                    content_json
                )
                VALUES
                (
                    @section_name,
                    @content_json
                );
                SELECT LAST_INSERT_ID();", conn);

            cmd.Parameters.AddWithValue("@section_name", request.SectionName);
            cmd.Parameters.AddWithValue("@content_json", request.ContentJson);

            var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Sección creada correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("{sectionName}")]
        public async Task<IActionResult> Update(string sectionName, [FromBody] HomeSectionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            long? id = null;

            using (var find = new MySqlCommand(@"
                SELECT id
                FROM home_sections
                WHERE section_name = @section_name
                LIMIT 1;", conn))
            {
                find.Parameters.AddWithValue("@section_name", sectionName);
                var obj = await find.ExecuteScalarAsync();

                if (obj == null)
                    return NotFound(new { message = "No existe una sección con ese nombre." });

                id = Convert.ToInt64(obj);
            }

            using var cmd = new MySqlCommand(@"
                UPDATE home_sections
                SET
                    content_json = @content_json,
                    updated_at = NOW()
                WHERE id = @id;", conn);

            cmd.Parameters.AddWithValue("@content_json", request.ContentJson);
            cmd.Parameters.AddWithValue("@id", id.Value);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                message = "Sección actualizada correctamente.",
                id = id.Value
            });
        }

        
        [Authorize]
        [HttpDelete("{sectionName}")]
        public async Task<IActionResult> Delete(string sectionName)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                DELETE FROM home_sections
                WHERE section_name = @section_name;", conn);

            cmd.Parameters.AddWithValue("@section_name", sectionName);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No existe una sección con ese nombre." });

            return Ok(new { message = "Sección eliminada correctamente." });
        }
    }
}