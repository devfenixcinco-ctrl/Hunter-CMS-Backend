using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BenefitsHeroController : ControllerBase
    {
        private readonly Db _db;

        public BenefitsHeroController(Db db)
        {
            _db = db;
        }

        private bool HasInvalidRequiredFields(BenefitsHeroRequest req)
        {
            return string.IsNullOrWhiteSpace(req.Title) ||
                   string.IsNullOrWhiteSpace(req.Subtitle) ||
                   string.IsNullOrWhiteSpace(req.HighlightText);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, title, subtitle, highlight_text, image_url, is_active, created_at
                        FROM benefits_hero
                        LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay hero configurado." });

            var item = new BenefitsHero
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Subtitle = reader["subtitle"]?.ToString(),
                HighlightText = reader["highlight_text"]?.ToString(),
                ImageUrl = reader["image_url"]?.ToString(),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] BenefitsHeroRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(request))
                return BadRequest(new
                {
                    message = "Title, Subtitle y HighlightText son obligatorios y no pueden estar vacíos."
                });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id, image_url FROM benefits_hero LIMIT 1;", conn);
            using var reader = await getIdCmd.ExecuteReaderAsync();

            int? existingId = null;
            string? currentImageUrl = null;

            if (await reader.ReadAsync())
            {
                existingId = reader.GetInt32("id");
                currentImageUrl = reader["image_url"] == DBNull.Value ? null : reader["image_url"]?.ToString();
            }

            await reader.CloseAsync();

            string? imageUrl = currentImageUrl;

            if (request.Image != null)
            {
                var fileNameBase = existingId.HasValue
                    ? $"benefit_{existingId.Value}"
                    : $"benefit_{Guid.NewGuid()}";

                imageUrl = await SaveImage(request.Image, "benefitshero", fileNameBase);
            }

            if (existingId.HasValue)
            {
                var updateSql = @"UPDATE benefits_hero
                                  SET title = @title,
                                      subtitle = @subtitle,
                                      highlight_text = @highlightText,
                                      image_url = @imageUrl,
                                      is_active = @isActive
                                  WHERE id = @id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@title", request.Title!.Trim());
                cmd.Parameters.AddWithValue("@subtitle", request.Subtitle!.Trim());
                cmd.Parameters.AddWithValue("@highlightText", request.HighlightText!.Trim());
                cmd.Parameters.AddWithValue("@imageUrl", (object?)imageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isActive", request.IsActive);
                cmd.Parameters.AddWithValue("@id", existingId.Value);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Hero actualizado",
                    created = false,
                    id = existingId.Value,
                    imageUrl
                });
            }

            var insertSql = @"INSERT INTO benefits_hero
                              (title, subtitle, highlight_text, image_url, is_active)
                              VALUES
                              (@title, @subtitle, @highlightText, @imageUrl, @isActive);
                              SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@title", request.Title!.Trim());
            insertCmd.Parameters.AddWithValue("@subtitle", request.Subtitle!.Trim());
            insertCmd.Parameters.AddWithValue("@highlightText", request.HighlightText!.Trim());
            insertCmd.Parameters.AddWithValue("@imageUrl", (object?)imageUrl ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@isActive", request.IsActive);

            var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Hero creado",
                created = true,
                id = newId,
                imageUrl
            });
        }

        [Authorize]
        [HttpPut]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] BenefitsHeroRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(request))
                return BadRequest(new
                {
                    message = "Title, Subtitle y HighlightText son obligatorios y no pueden estar vacíos."
                });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id, image_url FROM benefits_hero LIMIT 1;", conn);
            using var reader = await getIdCmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No existe hero para actualizar." });

            int id = reader.GetInt32("id");
            string? imageUrl = reader["image_url"] == DBNull.Value ? null : reader["image_url"]?.ToString();

            await reader.CloseAsync();

            if (request.Image != null)
            {
                imageUrl = await SaveImage(request.Image, "benefitshero", $"benefit_{id}");
            }

            var sql = @"UPDATE benefits_hero
                        SET title = @title,
                            subtitle = @subtitle,
                            highlight_text = @highlightText,
                            image_url = @imageUrl,
                            is_active = @isActive
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", request.Title!.Trim());
            cmd.Parameters.AddWithValue("@subtitle", request.Subtitle!.Trim());
            cmd.Parameters.AddWithValue("@highlightText", request.HighlightText!.Trim());
            cmd.Parameters.AddWithValue("@imageUrl", (object?)imageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isActive", request.IsActive);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                message = "Hero actualizado",
                id,
                imageUrl
            });
        }

        private async Task<string> SaveImage(IFormFile file, string folder, string fileNameWithoutExtension)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No se recibió ningún archivo.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new Exception("Formato no permitido. Solo jpg, jpeg, png y webp.");

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                folder
            );

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var ext in allowedExtensions)
            {
                var oldFile = Path.Combine(uploadsFolder, fileNameWithoutExtension + ext);
                if (System.IO.File.Exists(oldFile))
                    System.IO.File.Delete(oldFile);
            }

            var finalFileName = fileNameWithoutExtension + extension;
            var filePath = Path.Combine(uploadsFolder, finalFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return $"{baseUrl}/uploads/{folder}/{finalFileName}";
        }
    }
}