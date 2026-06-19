using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MreHunter.Api.Services;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/products-dashcam")]
    public class ProductsDashcamController : ControllerBase
    {
        private readonly Db _db;
        private readonly ProductsDashcamService _service;

        public ProductsDashcamController(Db db, ProductsDashcamService service)
        {
            _db = db;
            _service = service;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
            => reader[column] == DBNull.Value ? null : reader[column].ToString();

        private async Task<int?> GetLatestId(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand("SELECT id FROM products_dashcam ORDER BY id DESC LIMIT 1;", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT * FROM products_dashcam ORDER BY id DESC LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay configuración de ProductsDashcam aún" });

            return Ok(new
            {
                id = reader.GetInt32("id"),
                heroImageUrl = ReadNullableString(reader, "hero_image_url"),
                heroTitle = ReadNullableString(reader, "hero_title"),
                heroHighlightText = ReadNullableString(reader, "hero_highlight_text"),
                heroSubtitle = ReadNullableString(reader, "hero_subtitle"),
                benefitsTitle = ReadNullableString(reader, "benefits_title"),
                reasonTitle = ReadNullableString(reader, "reason_title"),
                reasonDescription = ReadNullableString(reader, "reason_description"),
                paymentTitle = ReadNullableString(reader, "payment_title"),
                primaryButtonText = ReadNullableString(reader, "primary_button_text"),
                primaryButtonLink = ReadNullableString(reader, "primary_button_link"),
                secondaryButtonText = ReadNullableString(reader, "secondary_button_text"),
                secondaryButtonLink = ReadNullableString(reader, "secondary_button_link"),
                createdAt = reader.GetDateTime("created_at")
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Save([FromForm] ProductsDashcamUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string? heroImageUrl = req.HeroImageUrl;

            if (req.HeroImage != null)
                heroImageUrl = await _service.SaveImageAsync(req.HeroImage, "productsdashcam");

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var existingId = await GetLatestId(conn);

            if (existingId.HasValue)
            {
                const string updateSql = @"UPDATE products_dashcam
                                           SET hero_image_url=@heroImageUrl,
                                               hero_title=@heroTitle,
                                               hero_highlight_text=@heroHighlightText,
                                               hero_subtitle=@heroSubtitle,
                                               benefits_title=@benefitsTitle,
                                               reason_title=@reasonTitle,
                                               reason_description=@reasonDescription,
                                               payment_title=@paymentTitle,
                                               primary_button_text=@primaryButtonText,
                                               primary_button_link=@primaryButtonLink,
                                               secondary_button_text=@secondaryButtonText,
                                               secondary_button_link=@secondaryButtonLink
                                           WHERE id=@id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@heroImageUrl", _service.NormalizeValue(heroImageUrl));
                cmd.Parameters.AddWithValue("@heroTitle", _service.NormalizeValue(req.HeroTitle));
                cmd.Parameters.AddWithValue("@heroHighlightText", _service.NormalizeValue(req.HeroHighlightText));
                cmd.Parameters.AddWithValue("@heroSubtitle", _service.NormalizeValue(req.HeroSubtitle));
                cmd.Parameters.AddWithValue("@benefitsTitle", _service.NormalizeValue(req.BenefitsTitle));
                cmd.Parameters.AddWithValue("@reasonTitle", _service.NormalizeValue(req.ReasonTitle));
                cmd.Parameters.AddWithValue("@reasonDescription", _service.NormalizeValue(req.ReasonDescription));
                cmd.Parameters.AddWithValue("@paymentTitle", _service.NormalizeValue(req.PaymentTitle));
                cmd.Parameters.AddWithValue("@primaryButtonText", _service.NormalizeValue(req.PrimaryButtonText));
                cmd.Parameters.AddWithValue("@primaryButtonLink", _service.NormalizeValue(req.PrimaryButtonLink));
                cmd.Parameters.AddWithValue("@secondaryButtonText", _service.NormalizeValue(req.SecondaryButtonText));
                cmd.Parameters.AddWithValue("@secondaryButtonLink", _service.NormalizeValue(req.SecondaryButtonLink));
                cmd.Parameters.AddWithValue("@id", existingId.Value);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return NotFound(new { message = "No se pudo actualizar ProductsDashcam" });

                return Ok(new
                {
                    message = "ProductsDashcam actualizado",
                    created = false,
                    id = existingId.Value,
                    heroImageUrl
                });
            }

            const string insertSql = @"INSERT INTO products_dashcam
                                       (hero_image_url, hero_title, hero_highlight_text, hero_subtitle,
                                        benefits_title,
                                        reason_title, reason_description,
                                        payment_title,
                                        primary_button_text, primary_button_link,
                                        secondary_button_text, secondary_button_link)
                                       VALUES
                                       (@heroImageUrl, @heroTitle, @heroHighlightText, @heroSubtitle,
                                        @benefitsTitle,
                                        @reasonTitle, @reasonDescription,
                                        @paymentTitle,
                                        @primaryButtonText, @primaryButtonLink,
                                        @secondaryButtonText, @secondaryButtonLink);
                                       SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@heroImageUrl", _service.NormalizeValue(heroImageUrl));
            insertCmd.Parameters.AddWithValue("@heroTitle", _service.NormalizeValue(req.HeroTitle));
            insertCmd.Parameters.AddWithValue("@heroHighlightText", _service.NormalizeValue(req.HeroHighlightText));
            insertCmd.Parameters.AddWithValue("@heroSubtitle", _service.NormalizeValue(req.HeroSubtitle));
            insertCmd.Parameters.AddWithValue("@benefitsTitle", _service.NormalizeValue(req.BenefitsTitle));
            insertCmd.Parameters.AddWithValue("@reasonTitle", _service.NormalizeValue(req.ReasonTitle));
            insertCmd.Parameters.AddWithValue("@reasonDescription", _service.NormalizeValue(req.ReasonDescription));
            insertCmd.Parameters.AddWithValue("@paymentTitle", _service.NormalizeValue(req.PaymentTitle));
            insertCmd.Parameters.AddWithValue("@primaryButtonText", _service.NormalizeValue(req.PrimaryButtonText));
            insertCmd.Parameters.AddWithValue("@primaryButtonLink", _service.NormalizeValue(req.PrimaryButtonLink));
            insertCmd.Parameters.AddWithValue("@secondaryButtonText", _service.NormalizeValue(req.SecondaryButtonText));
            insertCmd.Parameters.AddWithValue("@secondaryButtonLink", _service.NormalizeValue(req.SecondaryButtonLink));

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "ProductsDashcam creado",
                created = true,
                id = newId,
                heroImageUrl
            });
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromForm] ProductsDashcamUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe ProductsDashcam para actualizar" });

            string? heroImageUrl = req.HeroImageUrl;

            if (req.HeroImage != null)
                heroImageUrl = await _service.SaveImageAsync(req.HeroImage, "productsdashcam");

            const string sql = @"UPDATE products_dashcam
                                 SET hero_image_url=@heroImageUrl,
                                     hero_title=@heroTitle,
                                     hero_highlight_text=@heroHighlightText,
                                     hero_subtitle=@heroSubtitle,
                                     benefits_title=@benefitsTitle,
                                     reason_title=@reasonTitle,
                                     reason_description=@reasonDescription,
                                     payment_title=@paymentTitle,
                                     primary_button_text=@primaryButtonText,
                                     primary_button_link=@primaryButtonLink,
                                     secondary_button_text=@secondaryButtonText,
                                     secondary_button_link=@secondaryButtonLink
                                 WHERE id=@id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@heroImageUrl", _service.NormalizeValue(heroImageUrl));
            cmd.Parameters.AddWithValue("@heroTitle", _service.NormalizeValue(req.HeroTitle));
            cmd.Parameters.AddWithValue("@heroHighlightText", _service.NormalizeValue(req.HeroHighlightText));
            cmd.Parameters.AddWithValue("@heroSubtitle", _service.NormalizeValue(req.HeroSubtitle));
            cmd.Parameters.AddWithValue("@benefitsTitle", _service.NormalizeValue(req.BenefitsTitle));
            cmd.Parameters.AddWithValue("@reasonTitle", _service.NormalizeValue(req.ReasonTitle));
            cmd.Parameters.AddWithValue("@reasonDescription", _service.NormalizeValue(req.ReasonDescription));
            cmd.Parameters.AddWithValue("@paymentTitle", _service.NormalizeValue(req.PaymentTitle));
            cmd.Parameters.AddWithValue("@primaryButtonText", _service.NormalizeValue(req.PrimaryButtonText));
            cmd.Parameters.AddWithValue("@primaryButtonLink", _service.NormalizeValue(req.PrimaryButtonLink));
            cmd.Parameters.AddWithValue("@secondaryButtonText", _service.NormalizeValue(req.SecondaryButtonText));
            cmd.Parameters.AddWithValue("@secondaryButtonLink", _service.NormalizeValue(req.SecondaryButtonLink));
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo actualizar ProductsDashcam" });

            var fullHeroImageUrl = !string.IsNullOrEmpty(heroImageUrl)
                ? $"{Request.Scheme}://{Request.Host}{heroImageUrl}"
                : null;

            return Ok(new
            {
                message = "ProductsDashcam actualizado (PUT)",
                id = latestId.Value,
                heroImageUrl = fullHeroImageUrl
            });
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe ProductsDashcam para eliminar" });

            using var cmd = new MySqlCommand("DELETE FROM products_dashcam WHERE id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo eliminar ProductsDashcam" });

            return Ok(new { message = "ProductsDashcam eliminado", id = latestId.Value });
        }
    }
}