using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;
using Swashbuckle.AspNetCore.Filters;
using MreHunter.Api.Models.Examples;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/faqconfig")]
    [Tags("Preguntas Frecuentes")]
    public class FaqConfigController : ControllerBase
    {
        private readonly Db _db;

        public FaqConfigController(Db db)
        {
            _db = db;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
            => reader[column] == DBNull.Value ? null : reader[column].ToString();

        private static bool IsInvalidString(string? value)
            => string.IsNullOrWhiteSpace(value);

        private static object NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private async Task<int?> GetLatestFaqId(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand("SELECT id FROM faq_settings ORDER BY id DESC LIMIT 1;", conn);
            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

        private async Task<string?> GetCurrentHeroImageUrl(MySqlConnection conn)
        {
            const string sql = "SELECT hero_image_url FROM faq_settings ORDER BY id DESC LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? null : result.ToString();
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "faq");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/faq/{fileName}";
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT * FROM faq_settings ORDER BY id DESC LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay configuración FAQ aún" });

            return Ok(new
            {
                id = reader.GetInt32("id"),
                heroImageUrl = ReadNullableString(reader, "hero_image_url"),
                heroTitle = ReadNullableString(reader, "hero_title"),
                heroHighlightText = ReadNullableString(reader, "hero_highlight_text"),
                heroSubtitle = ReadNullableString(reader, "hero_subtitle"),
                question1 = ReadNullableString(reader, "question_1"),
                answer1 = ReadNullableString(reader, "answer_1"),
                question2 = ReadNullableString(reader, "question_2"),
                answer2 = ReadNullableString(reader, "answer_2"),
                question3 = ReadNullableString(reader, "question_3"),
                answer3 = ReadNullableString(reader, "answer_3"),
                question4 = ReadNullableString(reader, "question_4"),
                answer4 = ReadNullableString(reader, "answer_4"),
                question5 = ReadNullableString(reader, "question_5"),
                answer5 = ReadNullableString(reader, "answer_5"),
                question6 = ReadNullableString(reader, "question_6"),
                answer6 = ReadNullableString(reader, "answer_6"),
                question7 = ReadNullableString(reader, "question_7"),
                answer7 = ReadNullableString(reader, "answer_7"),
                question8 = ReadNullableString(reader, "question_8"),
                answer8 = ReadNullableString(reader, "answer_8"),
                question9 = ReadNullableString(reader, "question_9"),
                answer9 = ReadNullableString(reader, "answer_9"),
                question10 = ReadNullableString(reader, "question_10"),
                answer10 = ReadNullableString(reader, "answer_10"),
                question11 = ReadNullableString(reader, "question_11"),
                answer11 = ReadNullableString(reader, "answer_11"),
                question12 = ReadNullableString(reader, "question_12"),
                answer12 = ReadNullableString(reader, "answer_12"),
                question13 = ReadNullableString(reader, "question_13"),
                answer13 = ReadNullableString(reader, "answer_13"),
                createdAt = reader.GetDateTime("created_at")
            });
        }

        [Authorize]
        [SwaggerRequestExample(typeof(FaqSettingsUpdateRequest), typeof(FaqSettingsUpdateRequestExample))]
        [HttpPost]
        public async Task<IActionResult> Save([FromForm] FaqSettingsUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.HeroTitle) ||
                IsInvalidString(req.HeroHighlightText) ||
                IsInvalidString(req.HeroSubtitle) ||
                IsInvalidString(req.Question1) || IsInvalidString(req.Answer1) ||
                IsInvalidString(req.Question2) || IsInvalidString(req.Answer2) ||
                IsInvalidString(req.Question3) || IsInvalidString(req.Answer3) ||
                IsInvalidString(req.Question4) || IsInvalidString(req.Answer4) ||
                IsInvalidString(req.Question5) || IsInvalidString(req.Answer5) ||
                IsInvalidString(req.Question6) || IsInvalidString(req.Answer6) ||
                IsInvalidString(req.Question7) || IsInvalidString(req.Answer7) ||
                IsInvalidString(req.Question8) || IsInvalidString(req.Answer8) ||
                IsInvalidString(req.Question9) || IsInvalidString(req.Answer9) ||
                IsInvalidString(req.Question10) || IsInvalidString(req.Answer10) ||
                IsInvalidString(req.Question11) || IsInvalidString(req.Answer11) ||
                IsInvalidString(req.Question12) || IsInvalidString(req.Answer12) ||
                IsInvalidString(req.Question13) || IsInvalidString(req.Answer13))
            {
                return BadRequest(new { message = "Todos los campos del FAQ son obligatorios" });
            }

            string? heroImageUrl = req.HeroImageUrl;

            if (req.HeroImage != null)
            {
                heroImageUrl = await SaveImageAsync(req.HeroImage);
            }

            if (IsInvalidString(heroImageUrl))
                return BadRequest(new { message = "La imagen del FAQ es obligatoria" });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var existingId = await GetLatestFaqId(conn);

            if (existingId.HasValue)
            {
                const string updateSql = @"UPDATE faq_settings
                                           SET hero_image_url=@heroImageUrl,
                                               hero_title=@heroTitle,
                                               hero_highlight_text=@heroHighlightText,
                                               hero_subtitle=@heroSubtitle,
                                               question_1=@q1, answer_1=@a1,
                                               question_2=@q2, answer_2=@a2,
                                               question_3=@q3, answer_3=@a3,
                                               question_4=@q4, answer_4=@a4,
                                               question_5=@q5, answer_5=@a5,
                                               question_6=@q6, answer_6=@a6,
                                               question_7=@q7, answer_7=@a7,
                                               question_8=@q8, answer_8=@a8,
                                               question_9=@q9, answer_9=@a9,
                                               question_10=@q10, answer_10=@a10,
                                               question_11=@q11, answer_11=@a11,
                                               question_12=@q12, answer_12=@a12,
                                               question_13=@q13, answer_13=@a13
                                           WHERE id=@id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@heroImageUrl", NormalizeValue(heroImageUrl));
                cmd.Parameters.AddWithValue("@heroTitle", NormalizeValue(req.HeroTitle));
                cmd.Parameters.AddWithValue("@heroHighlightText", NormalizeValue(req.HeroHighlightText));
                cmd.Parameters.AddWithValue("@heroSubtitle", NormalizeValue(req.HeroSubtitle));
                cmd.Parameters.AddWithValue("@q1", NormalizeValue(req.Question1));
                cmd.Parameters.AddWithValue("@a1", NormalizeValue(req.Answer1));
                cmd.Parameters.AddWithValue("@q2", NormalizeValue(req.Question2));
                cmd.Parameters.AddWithValue("@a2", NormalizeValue(req.Answer2));
                cmd.Parameters.AddWithValue("@q3", NormalizeValue(req.Question3));
                cmd.Parameters.AddWithValue("@a3", NormalizeValue(req.Answer3));
                cmd.Parameters.AddWithValue("@q4", NormalizeValue(req.Question4));
                cmd.Parameters.AddWithValue("@a4", NormalizeValue(req.Answer4));
                cmd.Parameters.AddWithValue("@q5", NormalizeValue(req.Question5));
                cmd.Parameters.AddWithValue("@a5", NormalizeValue(req.Answer5));
                cmd.Parameters.AddWithValue("@q6", NormalizeValue(req.Question6));
                cmd.Parameters.AddWithValue("@a6", NormalizeValue(req.Answer6));
                cmd.Parameters.AddWithValue("@q7", NormalizeValue(req.Question7));
                cmd.Parameters.AddWithValue("@a7", NormalizeValue(req.Answer7));
                cmd.Parameters.AddWithValue("@q8", NormalizeValue(req.Question8));
                cmd.Parameters.AddWithValue("@a8", NormalizeValue(req.Answer8));
                cmd.Parameters.AddWithValue("@q9", NormalizeValue(req.Question9));
                cmd.Parameters.AddWithValue("@a9", NormalizeValue(req.Answer9));
                cmd.Parameters.AddWithValue("@q10", NormalizeValue(req.Question10));
                cmd.Parameters.AddWithValue("@a10", NormalizeValue(req.Answer10));
                cmd.Parameters.AddWithValue("@q11", NormalizeValue(req.Question11));
                cmd.Parameters.AddWithValue("@a11", NormalizeValue(req.Answer11));
                cmd.Parameters.AddWithValue("@q12", NormalizeValue(req.Question12));
                cmd.Parameters.AddWithValue("@a12", NormalizeValue(req.Answer12));
                cmd.Parameters.AddWithValue("@q13", NormalizeValue(req.Question13));
                cmd.Parameters.AddWithValue("@a13", NormalizeValue(req.Answer13));
                cmd.Parameters.AddWithValue("@id", existingId.Value);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return NotFound(new { message = "No se pudo actualizar la configuración FAQ" });

                return Ok(new
                {
                    message = "FAQ actualizada",
                    created = false,
                    id = existingId.Value,
                    heroImageUrl
                });
            }

            const string insertSql = @"INSERT INTO faq_settings
                                       (hero_image_url, hero_title, hero_highlight_text, hero_subtitle,
                                        question_1, answer_1, question_2, answer_2, question_3, answer_3,
                                        question_4, answer_4, question_5, answer_5, question_6, answer_6,
                                        question_7, answer_7, question_8, answer_8, question_9, answer_9,
                                        question_10, answer_10, question_11, answer_11, question_12, answer_12,
                                        question_13, answer_13)
                                       VALUES
                                       (@heroImageUrl, @heroTitle, @heroHighlightText, @heroSubtitle,
                                        @q1, @a1, @q2, @a2, @q3, @a3, @q4, @a4, @q5, @a5, @q6, @a6,
                                        @q7, @a7, @q8, @a8, @q9, @a9, @q10, @a10, @q11, @a11, @q12, @a12,
                                        @q13, @a13);
                                       SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@heroImageUrl", NormalizeValue(heroImageUrl));
            insertCmd.Parameters.AddWithValue("@heroTitle", NormalizeValue(req.HeroTitle));
            insertCmd.Parameters.AddWithValue("@heroHighlightText", NormalizeValue(req.HeroHighlightText));
            insertCmd.Parameters.AddWithValue("@heroSubtitle", NormalizeValue(req.HeroSubtitle));
            insertCmd.Parameters.AddWithValue("@q1", NormalizeValue(req.Question1));
            insertCmd.Parameters.AddWithValue("@a1", NormalizeValue(req.Answer1));
            insertCmd.Parameters.AddWithValue("@q2", NormalizeValue(req.Question2));
            insertCmd.Parameters.AddWithValue("@a2", NormalizeValue(req.Answer2));
            insertCmd.Parameters.AddWithValue("@q3", NormalizeValue(req.Question3));
            insertCmd.Parameters.AddWithValue("@a3", NormalizeValue(req.Answer3));
            insertCmd.Parameters.AddWithValue("@q4", NormalizeValue(req.Question4));
            insertCmd.Parameters.AddWithValue("@a4", NormalizeValue(req.Answer4));
            insertCmd.Parameters.AddWithValue("@q5", NormalizeValue(req.Question5));
            insertCmd.Parameters.AddWithValue("@a5", NormalizeValue(req.Answer5));
            insertCmd.Parameters.AddWithValue("@q6", NormalizeValue(req.Question6));
            insertCmd.Parameters.AddWithValue("@a6", NormalizeValue(req.Answer6));
            insertCmd.Parameters.AddWithValue("@q7", NormalizeValue(req.Question7));
            insertCmd.Parameters.AddWithValue("@a7", NormalizeValue(req.Answer7));
            insertCmd.Parameters.AddWithValue("@q8", NormalizeValue(req.Question8));
            insertCmd.Parameters.AddWithValue("@a8", NormalizeValue(req.Answer8));
            insertCmd.Parameters.AddWithValue("@q9", NormalizeValue(req.Question9));
            insertCmd.Parameters.AddWithValue("@a9", NormalizeValue(req.Answer9));
            insertCmd.Parameters.AddWithValue("@q10", NormalizeValue(req.Question10));
            insertCmd.Parameters.AddWithValue("@a10", NormalizeValue(req.Answer10));
            insertCmd.Parameters.AddWithValue("@q11", NormalizeValue(req.Question11));
            insertCmd.Parameters.AddWithValue("@a11", NormalizeValue(req.Answer11));
            insertCmd.Parameters.AddWithValue("@q12", NormalizeValue(req.Question12));
            insertCmd.Parameters.AddWithValue("@a12", NormalizeValue(req.Answer12));
            insertCmd.Parameters.AddWithValue("@q13", NormalizeValue(req.Question13));
            insertCmd.Parameters.AddWithValue("@a13", NormalizeValue(req.Answer13));

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "FAQ creada",
                created = true,
                id = newId,
                heroImageUrl
            });
        }

        [Authorize]
        [SwaggerRequestExample(typeof(FaqSettingsUpdateRequest), typeof(FaqSettingsUpdateRequestExample))]
        [HttpPut]
        public async Task<IActionResult> Update([FromForm] FaqSettingsUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.HeroTitle) ||
                IsInvalidString(req.HeroHighlightText) ||
                IsInvalidString(req.HeroSubtitle) ||
                IsInvalidString(req.Question1) || IsInvalidString(req.Answer1) ||
                IsInvalidString(req.Question2) || IsInvalidString(req.Answer2) ||
                IsInvalidString(req.Question3) || IsInvalidString(req.Answer3) ||
                IsInvalidString(req.Question4) || IsInvalidString(req.Answer4) ||
                IsInvalidString(req.Question5) || IsInvalidString(req.Answer5) ||
                IsInvalidString(req.Question6) || IsInvalidString(req.Answer6) ||
                IsInvalidString(req.Question7) || IsInvalidString(req.Answer7) ||
                IsInvalidString(req.Question8) || IsInvalidString(req.Answer8) ||
                IsInvalidString(req.Question9) || IsInvalidString(req.Answer9) ||
                IsInvalidString(req.Question10) || IsInvalidString(req.Answer10) ||
                IsInvalidString(req.Question11) || IsInvalidString(req.Answer11) ||
                IsInvalidString(req.Question12) || IsInvalidString(req.Answer12) ||
                IsInvalidString(req.Question13) || IsInvalidString(req.Answer13))
            {
                return BadRequest(new { message = "Todos los campos del FAQ son obligatorios" });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestFaqId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe configuración FAQ para actualizar" });

            string? heroImageUrl = req.HeroImageUrl;

            if (req.HeroImage != null)
            {
                heroImageUrl = await SaveImageAsync(req.HeroImage);
            }
            else if (IsInvalidString(heroImageUrl))
            {
                heroImageUrl = await GetCurrentHeroImageUrl(conn);
            }

            if (IsInvalidString(heroImageUrl))
                return BadRequest(new { message = "La imagen del FAQ es obligatoria" });

            const string sql = @"UPDATE faq_settings
                                 SET hero_image_url=@heroImageUrl,
                                     hero_title=@heroTitle,
                                     hero_highlight_text=@heroHighlightText,
                                     hero_subtitle=@heroSubtitle,
                                     question_1=@q1, answer_1=@a1,
                                     question_2=@q2, answer_2=@a2,
                                     question_3=@q3, answer_3=@a3,
                                     question_4=@q4, answer_4=@a4,
                                     question_5=@q5, answer_5=@a5,
                                     question_6=@q6, answer_6=@a6,
                                     question_7=@q7, answer_7=@a7,
                                     question_8=@q8, answer_8=@a8,
                                     question_9=@q9, answer_9=@a9,
                                     question_10=@q10, answer_10=@a10,
                                     question_11=@q11, answer_11=@a11,
                                     question_12=@q12, answer_12=@a12,
                                     question_13=@q13, answer_13=@a13
                                 WHERE id=@id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@heroImageUrl", NormalizeValue(heroImageUrl));
            cmd.Parameters.AddWithValue("@heroTitle", NormalizeValue(req.HeroTitle));
            cmd.Parameters.AddWithValue("@heroHighlightText", NormalizeValue(req.HeroHighlightText));
            cmd.Parameters.AddWithValue("@heroSubtitle", NormalizeValue(req.HeroSubtitle));
            cmd.Parameters.AddWithValue("@q1", NormalizeValue(req.Question1));
            cmd.Parameters.AddWithValue("@a1", NormalizeValue(req.Answer1));
            cmd.Parameters.AddWithValue("@q2", NormalizeValue(req.Question2));
            cmd.Parameters.AddWithValue("@a2", NormalizeValue(req.Answer2));
            cmd.Parameters.AddWithValue("@q3", NormalizeValue(req.Question3));
            cmd.Parameters.AddWithValue("@a3", NormalizeValue(req.Answer3));
            cmd.Parameters.AddWithValue("@q4", NormalizeValue(req.Question4));
            cmd.Parameters.AddWithValue("@a4", NormalizeValue(req.Answer4));
            cmd.Parameters.AddWithValue("@q5", NormalizeValue(req.Question5));
            cmd.Parameters.AddWithValue("@a5", NormalizeValue(req.Answer5));
            cmd.Parameters.AddWithValue("@q6", NormalizeValue(req.Question6));
            cmd.Parameters.AddWithValue("@a6", NormalizeValue(req.Answer6));
            cmd.Parameters.AddWithValue("@q7", NormalizeValue(req.Question7));
            cmd.Parameters.AddWithValue("@a7", NormalizeValue(req.Answer7));
            cmd.Parameters.AddWithValue("@q8", NormalizeValue(req.Question8));
            cmd.Parameters.AddWithValue("@a8", NormalizeValue(req.Answer8));
            cmd.Parameters.AddWithValue("@q9", NormalizeValue(req.Question9));
            cmd.Parameters.AddWithValue("@a9", NormalizeValue(req.Answer9));
            cmd.Parameters.AddWithValue("@q10", NormalizeValue(req.Question10));
            cmd.Parameters.AddWithValue("@a10", NormalizeValue(req.Answer10));
            cmd.Parameters.AddWithValue("@q11", NormalizeValue(req.Question11));
            cmd.Parameters.AddWithValue("@a11", NormalizeValue(req.Answer11));
            cmd.Parameters.AddWithValue("@q12", NormalizeValue(req.Question12));
            cmd.Parameters.AddWithValue("@a12", NormalizeValue(req.Answer12));
            cmd.Parameters.AddWithValue("@q13", NormalizeValue(req.Question13));
            cmd.Parameters.AddWithValue("@a13", NormalizeValue(req.Answer13));
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo actualizar la configuración FAQ" });

            return Ok(new
            {
                message = "FAQ actualizada (PUT)",
                id = latestId.Value,
                heroImageUrl
            });
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestFaqId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe configuración FAQ para eliminar" });

            using var cmd = new MySqlCommand("DELETE FROM faq_settings WHERE id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo eliminar la configuración FAQ" });

            return Ok(new { message = "FAQ eliminada", id = latestId.Value });
        }
    }
}