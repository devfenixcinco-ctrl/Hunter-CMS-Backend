using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BenefitContentBlockController : ControllerBase
    {
        private readonly Db _db;

        public BenefitContentBlockController(Db db)
        {
            _db = db;
        }

        private bool HasInvalidRequiredFields(BenefitContentBlockRequest req)
        {
            return string.IsNullOrWhiteSpace(req.Title) ||
                   string.IsNullOrWhiteSpace(req.Description) ||
                   string.IsNullOrWhiteSpace(req.MediaUrl) ||
                   string.IsNullOrWhiteSpace(req.MediaType) ||
                   string.IsNullOrWhiteSpace(req.TextPosition) ||
                   req.DisplayOrder <= 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, title, description, media_url, media_type, text_position, display_order, is_active, created_at
                        FROM benefit_content_blocks
                        ORDER BY display_order ASC, id ASC;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<BenefitContentBlock>();

            while (await reader.ReadAsync())
            {
                items.Add(new BenefitContentBlock
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Description = reader["description"]?.ToString(),
                    MediaUrl = reader["media_url"]?.ToString(),
                    MediaType = reader.GetString("media_type"),
                    TextPosition = reader.GetString("text_position"),
                    DisplayOrder = reader.GetInt32("display_order"),
                    IsActive = reader.GetBoolean("is_active"),
                    CreatedAt = reader.GetDateTime("created_at")
                });
            }

            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, title, description, media_url, media_type, text_position, display_order, is_active, created_at
                        FROM benefit_content_blocks
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "Bloque no encontrado." });

            var item = new BenefitContentBlock
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Description = reader["description"]?.ToString(),
                MediaUrl = reader["media_url"]?.ToString(),
                MediaType = reader.GetString("media_type"),
                TextPosition = reader.GetString("text_position"),
                DisplayOrder = reader.GetInt32("display_order"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BenefitContentBlockRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(req))
            {
                return BadRequest(new
                {
                    message = "Title, Description, MediaUrl, MediaType, TextPosition son obligatorios y DisplayOrder debe ser mayor a 0."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO benefit_content_blocks
                        (title, description, media_url, media_type, text_position, display_order, is_active)
                        VALUES
                        (@title, @description, @mediaUrl, @mediaType, @textPosition, @displayOrder, @isActive);
                        SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
            cmd.Parameters.AddWithValue("@mediaUrl", req.MediaUrl?.Trim());
            cmd.Parameters.AddWithValue("@mediaType", req.MediaType?.Trim());
            cmd.Parameters.AddWithValue("@textPosition", req.TextPosition?.Trim());
            cmd.Parameters.AddWithValue("@displayOrder", req.DisplayOrder);
            cmd.Parameters.AddWithValue("@isActive", req.IsActive);

            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Bloque creado correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BenefitContentBlockRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(req))
            {
                return BadRequest(new
                {
                    message = "Title, Description, MediaUrl, MediaType, TextPosition son obligatorios y DisplayOrder debe ser mayor a 0."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE benefit_content_blocks
                        SET title = @title,
                            description = @description,
                            media_url = @mediaUrl,
                            media_type = @mediaType,
                            text_position = @textPosition,
                            display_order = @displayOrder,
                            is_active = @isActive
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
            cmd.Parameters.AddWithValue("@mediaUrl", req.MediaUrl?.Trim());
            cmd.Parameters.AddWithValue("@mediaType", req.MediaType?.Trim());
            cmd.Parameters.AddWithValue("@textPosition", req.TextPosition?.Trim());
            cmd.Parameters.AddWithValue("@displayOrder", req.DisplayOrder);
            cmd.Parameters.AddWithValue("@isActive", req.IsActive);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Bloque no encontrado." });

            return Ok(new { message = "Bloque actualizado correctamente." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "DELETE FROM benefit_content_blocks WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Bloque no encontrado." });

            return Ok(new { message = "Bloque eliminado correctamente." });
        }
    }
}