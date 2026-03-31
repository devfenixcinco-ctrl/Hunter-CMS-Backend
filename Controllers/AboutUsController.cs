using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using System.Text.Json;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AboutUsController : ControllerBase
    {
        private readonly Db _db;

        public AboutUsController(Db db)
        {
            _db = db;
        }

        private bool HasEmptyRequiredFields(AboutUsRequest request)
        {
            return string.IsNullOrWhiteSpace(request.HeroTitle) ||
                   string.IsNullOrWhiteSpace(request.HeroText) ||
                   string.IsNullOrWhiteSpace(request.WhoTitle) ||
                   string.IsNullOrWhiteSpace(request.WhoText) ||
                   string.IsNullOrWhiteSpace(request.ChooseTitle) ||
                   string.IsNullOrWhiteSpace(request.ChooseText) ||
                   string.IsNullOrWhiteSpace(request.MissionTitle) ||
                   string.IsNullOrWhiteSpace(request.MissionText) ||
                   string.IsNullOrWhiteSpace(request.VisionTitle) ||
                   string.IsNullOrWhiteSpace(request.VisionText) ||
                   string.IsNullOrWhiteSpace(request.BenefitsTitle) ||
                   string.IsNullOrWhiteSpace(request.BenefitsSubtitle) ||
                   request.BenefitsItems == null ||
                   request.BenefitsItems.Count == 0 ||
                   request.BenefitsItems.Any(x =>
                       x == null ||
                       string.IsNullOrWhiteSpace(x.Icon) ||
                       string.IsNullOrWhiteSpace(x.Text)) ||
                   string.IsNullOrWhiteSpace(request.CtaTitle) ||
                   string.IsNullOrWhiteSpace(request.CtaButtonText) ||
                   string.IsNullOrWhiteSpace(request.Email);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT *
                FROM about_us
                ORDER BY id DESC
                LIMIT 1;", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay contenido de 'Nosotros' registrado." });

            var benefitsJson = reader["benefits_items_json"]?.ToString() ?? "[]";

            List<BenefitItem> benefits;
            try
            {
                benefits = JsonSerializer.Deserialize<List<BenefitItem>>(benefitsJson) ?? new List<BenefitItem>();
            }
            catch
            {
                benefits = new List<BenefitItem>();
            }

            return Ok(new
            {
                id = Convert.ToInt64(reader["id"]),

                heroTitle = reader["hero_title"]?.ToString(),
                heroText = reader["hero_text"]?.ToString(),
                heroVideoUrl = reader["hero_video_url"]?.ToString(),

                whoTitle = reader["who_title"]?.ToString(),
                whoText = reader["who_text"]?.ToString(),

                chooseTitle = reader["choose_title"]?.ToString(),
                chooseText = reader["choose_text"]?.ToString(),

                missionTitle = reader["mission_title"]?.ToString(),
                missionText = reader["mission_text"]?.ToString(),

                visionTitle = reader["vision_title"]?.ToString(),
                visionText = reader["vision_text"]?.ToString(),

                benefitsTitle = reader["benefits_title"]?.ToString(),
                benefitsSubtitle = reader["benefits_subtitle"]?.ToString(),
                benefitsItems = benefits,

                ctaTitle = reader["cta_title"]?.ToString(),
                ctaButtonText = reader["cta_button_text"]?.ToString(),
                ctaButtonLink = reader["cta_button_link"]?.ToString(),
                ctaImageUrl = reader["cta_image_url"]?.ToString(),

                email = reader["email"]?.ToString(),

                createdAt = reader["created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["created_at"]),
                updatedAt = reader["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["updated_at"]),
            });
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] AboutUsRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasEmptyRequiredFields(request))
                return BadRequest(new { message = "Todos los campos obligatorios deben tener un valor válido." });

            var now = DateTime.UtcNow;

            string? ctaImageUrl = null;
            if (request.CtaImage != null)
            {
                ctaImageUrl = await SaveImage(request.CtaImage, "aboutus", "cta_image");
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using (var check = new MySqlCommand("SELECT COUNT(*) FROM about_us;", conn))
            {
                var count = Convert.ToInt64(await check.ExecuteScalarAsync());
                if (count > 0)
                    return Conflict(new { message = "Ya existe contenido de 'Nosotros'. Usa PUT para actualizar." });
            }

            var benefitsJson = JsonSerializer.Serialize(request.BenefitsItems);

            using var cmd = new MySqlCommand(@"
                INSERT INTO about_us
                (
                    hero_title, hero_text, hero_video_url,
                    who_title, who_text,
                    choose_title, choose_text,
                    mission_title, mission_text,
                    vision_title, vision_text,
                    benefits_title, benefits_subtitle, benefits_items_json,
                    cta_title, cta_button_text, cta_button_link, cta_image_url,
                    email,
                    created_at, updated_at
                )
                VALUES
                (
                    @hero_title, @hero_text, @hero_video_url,
                    @who_title, @who_text,
                    @choose_title, @choose_text,
                    @mission_title, @mission_text,
                    @vision_title, @vision_text,
                    @benefits_title, @benefits_subtitle, @benefits_items_json,
                    @cta_title, @cta_button_text, @cta_button_link, @cta_image_url,
                    @email,
                    @now, @now
                );
                SELECT LAST_INSERT_ID();", conn);

            cmd.Parameters.AddWithValue("@hero_title", request.HeroTitle!.Trim());
            cmd.Parameters.AddWithValue("@hero_text", request.HeroText!.Trim());
            cmd.Parameters.AddWithValue("@hero_video_url", string.IsNullOrWhiteSpace(request.HeroVideoUrl) ? DBNull.Value : request.HeroVideoUrl.Trim());

            cmd.Parameters.AddWithValue("@who_title", request.WhoTitle!.Trim());
            cmd.Parameters.AddWithValue("@who_text", request.WhoText!.Trim());

            cmd.Parameters.AddWithValue("@choose_title", request.ChooseTitle!.Trim());
            cmd.Parameters.AddWithValue("@choose_text", request.ChooseText!.Trim());

            cmd.Parameters.AddWithValue("@mission_title", request.MissionTitle!.Trim());
            cmd.Parameters.AddWithValue("@mission_text", request.MissionText!.Trim());

            cmd.Parameters.AddWithValue("@vision_title", request.VisionTitle!.Trim());
            cmd.Parameters.AddWithValue("@vision_text", request.VisionText!.Trim());

            cmd.Parameters.AddWithValue("@benefits_title", request.BenefitsTitle!.Trim());
            cmd.Parameters.AddWithValue("@benefits_subtitle", request.BenefitsSubtitle!.Trim());
            cmd.Parameters.AddWithValue("@benefits_items_json", benefitsJson);

            cmd.Parameters.AddWithValue("@cta_title", request.CtaTitle!.Trim());
            cmd.Parameters.AddWithValue("@cta_button_text", request.CtaButtonText!.Trim());
            cmd.Parameters.AddWithValue("@cta_button_link", string.IsNullOrWhiteSpace(request.CtaButtonLink) ? DBNull.Value : request.CtaButtonLink.Trim());
            cmd.Parameters.AddWithValue("@cta_image_url", (object?)ctaImageUrl ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@email", request.Email!.Trim());
            cmd.Parameters.AddWithValue("@now", now);

            var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            return Ok(new { message = "Contenido 'Nosotros' creado.", id = newId, ctaImageUrl });
        }

        [Authorize]
        [HttpPut]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update([FromForm] AboutUsRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasEmptyRequiredFields(request))
                return BadRequest(new { message = "Todos los campos obligatorios deben tener un valor válido." });

            var now = DateTime.UtcNow;

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            long id;
            using (var find = new MySqlCommand("SELECT id FROM about_us ORDER BY id DESC LIMIT 1;", conn))
            {
                var obj = await find.ExecuteScalarAsync();
                if (obj == null)
                    return NotFound(new { message = "No existe contenido de 'Nosotros' para actualizar." });

                id = Convert.ToInt64(obj);
            }

            string? ctaImageUrl = null;

            using (var getCurrent = new MySqlCommand("SELECT cta_image_url FROM about_us WHERE id = @id;", conn))
            {
                getCurrent.Parameters.AddWithValue("@id", id);
                var currentObj = await getCurrent.ExecuteScalarAsync();
                ctaImageUrl = currentObj == DBNull.Value || currentObj == null ? null : currentObj.ToString();
            }

            if (request.CtaImage != null)
            {
                ctaImageUrl = await SaveImage(request.CtaImage, "aboutus", "cta_image");
            }

            var benefitsJson = JsonSerializer.Serialize(request.BenefitsItems);

            using var cmd = new MySqlCommand(@"
                UPDATE about_us
                SET
                    hero_title=@hero_title,
                    hero_text=@hero_text,
                    hero_video_url=@hero_video_url,

                    who_title=@who_title,
                    who_text=@who_text,

                    choose_title=@choose_title,
                    choose_text=@choose_text,

                    mission_title=@mission_title,
                    mission_text=@mission_text,

                    vision_title=@vision_title,
                    vision_text=@vision_text,

                    benefits_title=@benefits_title,
                    benefits_subtitle=@benefits_subtitle,
                    benefits_items_json=@benefits_items_json,

                    cta_title=@cta_title,
                    cta_button_text=@cta_button_text,
                    cta_button_link=@cta_button_link,
                    cta_image_url=@cta_image_url,
                    email=@email,

                    updated_at=@now
                WHERE id=@id;", conn);

            cmd.Parameters.AddWithValue("@hero_title", request.HeroTitle!.Trim());
            cmd.Parameters.AddWithValue("@hero_text", request.HeroText!.Trim());
            cmd.Parameters.AddWithValue("@hero_video_url", string.IsNullOrWhiteSpace(request.HeroVideoUrl) ? DBNull.Value : request.HeroVideoUrl.Trim());

            cmd.Parameters.AddWithValue("@who_title", request.WhoTitle!.Trim());
            cmd.Parameters.AddWithValue("@who_text", request.WhoText!.Trim());

            cmd.Parameters.AddWithValue("@choose_title", request.ChooseTitle!.Trim());
            cmd.Parameters.AddWithValue("@choose_text", request.ChooseText!.Trim());

            cmd.Parameters.AddWithValue("@mission_title", request.MissionTitle!.Trim());
            cmd.Parameters.AddWithValue("@mission_text", request.MissionText!.Trim());

            cmd.Parameters.AddWithValue("@vision_title", request.VisionTitle!.Trim());
            cmd.Parameters.AddWithValue("@vision_text", request.VisionText!.Trim());

            cmd.Parameters.AddWithValue("@benefits_title", request.BenefitsTitle!.Trim());
            cmd.Parameters.AddWithValue("@benefits_subtitle", request.BenefitsSubtitle!.Trim());
            cmd.Parameters.AddWithValue("@benefits_items_json", benefitsJson);

            cmd.Parameters.AddWithValue("@cta_title", request.CtaTitle!.Trim());
            cmd.Parameters.AddWithValue("@cta_button_text", request.CtaButtonText!.Trim());
            cmd.Parameters.AddWithValue("@cta_button_link", string.IsNullOrWhiteSpace(request.CtaButtonLink) ? DBNull.Value : request.CtaButtonLink.Trim());
            cmd.Parameters.AddWithValue("@cta_image_url", (object?)ctaImageUrl ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@email", request.Email!.Trim());

            cmd.Parameters.AddWithValue("@now", now);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
            return Ok(new { message = "Contenido 'Nosotros' actualizado.", id, ctaImageUrl });
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