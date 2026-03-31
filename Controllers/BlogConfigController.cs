using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/blogconfig")]
    public class BlogConfigController : ControllerBase
    {
        private readonly Db _db;

        public BlogConfigController(Db db)
        {
            _db = db;
        }

        private static string? ReadNullableString(MySqlDataReader reader, string column)
            => reader[column] == DBNull.Value ? null : reader[column].ToString();

        private static bool IsInvalidString(string? value)
            => string.IsNullOrWhiteSpace(value);

        private static object NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private async Task<int?> GetLatestBlogConfigId(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand(
                "SELECT id FROM blog_settings ORDER BY id DESC LIMIT 1;",
                conn
            );

            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Convert.ToInt32(result);
        }

    

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT * FROM blog_settings ORDER BY id DESC LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay configuración de blog aún" });

            return Ok(new
            {
                id = reader.GetInt32("id"),
                bannerTitle = ReadNullableString(reader, "banner_title"),
                bannerSubtitle = ReadNullableString(reader, "banner_subtitle"),
                subscriptionTitle = ReadNullableString(reader, "subscription_title"),
                subscriptionSubtitle = ReadNullableString(reader, "subscription_subtitle"),
                acceptanceText = ReadNullableString(reader, "acceptance_text"),
                createdAt = reader.GetDateTime("created_at")
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] BlogSettingsUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.BannerTitle) ||
                IsInvalidString(req.BannerSubtitle) ||
                IsInvalidString(req.SubscriptionTitle) ||
                IsInvalidString(req.SubscriptionSubtitle) ||
                IsInvalidString(req.AcceptanceText))
            {
                return BadRequest(new
                {
                    message = "Todos los campos son obligatorios y no pueden estar vacíos"
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var existingId = await GetLatestBlogConfigId(conn);

            if (existingId.HasValue)
            {
                const string updateSql = @"UPDATE blog_settings
                                           SET banner_title=@bannerTitle,
                                               banner_subtitle=@bannerSubtitle,
                                               subscription_title=@subscriptionTitle,
                                               subscription_subtitle=@subscriptionSubtitle,
                                               acceptance_text=@acceptanceText
                                           WHERE id=@id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@bannerTitle", NormalizeValue(req.BannerTitle));
                cmd.Parameters.AddWithValue("@bannerSubtitle", NormalizeValue(req.BannerSubtitle));
                cmd.Parameters.AddWithValue("@subscriptionTitle", NormalizeValue(req.SubscriptionTitle));
                cmd.Parameters.AddWithValue("@subscriptionSubtitle", NormalizeValue(req.SubscriptionSubtitle));
                cmd.Parameters.AddWithValue("@acceptanceText", NormalizeValue(req.AcceptanceText));
                cmd.Parameters.AddWithValue("@id", existingId.Value);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    return NotFound(new { message = "No se pudo actualizar la configuración del blog" });

                return Ok(new
                {
                    message = "Blog config actualizado",
                    created = false,
                    id = existingId.Value
                });
            }

            const string insertSql = @"INSERT INTO blog_settings
                                       (banner_title, banner_subtitle, subscription_title, subscription_subtitle, acceptance_text)
                                       VALUES
                                       (@bannerTitle, @bannerSubtitle, @subscriptionTitle, @subscriptionSubtitle, @acceptanceText);
                                       SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@bannerTitle", NormalizeValue(req.BannerTitle));
            insertCmd.Parameters.AddWithValue("@bannerSubtitle", NormalizeValue(req.BannerSubtitle));
            insertCmd.Parameters.AddWithValue("@subscriptionTitle", NormalizeValue(req.SubscriptionTitle));
            insertCmd.Parameters.AddWithValue("@subscriptionSubtitle", NormalizeValue(req.SubscriptionSubtitle));
            insertCmd.Parameters.AddWithValue("@acceptanceText", NormalizeValue(req.AcceptanceText));

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Blog config creado",
                created = true,
                id = newId
            });
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] BlogSettingsUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "El body es requerido" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (IsInvalidString(req.BannerTitle) ||
                IsInvalidString(req.BannerSubtitle) ||
                IsInvalidString(req.SubscriptionTitle) ||
                IsInvalidString(req.SubscriptionSubtitle) ||
                IsInvalidString(req.AcceptanceText))
            {
                return BadRequest(new
                {
                    message = "Todos los campos son obligatorios y no pueden estar vacíos"
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestBlogConfigId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe configuración del blog para actualizar" });

            const string sql = @"UPDATE blog_settings
                                 SET banner_title=@bannerTitle,
                                     banner_subtitle=@bannerSubtitle,
                                     subscription_title=@subscriptionTitle,
                                     subscription_subtitle=@subscriptionSubtitle,
                                     acceptance_text=@acceptanceText
                                 WHERE id=@id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@bannerTitle", NormalizeValue(req.BannerTitle));
            cmd.Parameters.AddWithValue("@bannerSubtitle", NormalizeValue(req.BannerSubtitle));
            cmd.Parameters.AddWithValue("@subscriptionTitle", NormalizeValue(req.SubscriptionTitle));
            cmd.Parameters.AddWithValue("@subscriptionSubtitle", NormalizeValue(req.SubscriptionSubtitle));
            cmd.Parameters.AddWithValue("@acceptanceText", NormalizeValue(req.AcceptanceText));
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo actualizar la configuración del blog" });

            return Ok(new
            {
                message = "Blog config actualizado (PUT)",
                id = latestId.Value
            });
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var latestId = await GetLatestBlogConfigId(conn);
            if (!latestId.HasValue)
                return NotFound(new { message = "No existe configuración del blog para eliminar" });

            using var cmd = new MySqlCommand("DELETE FROM blog_settings WHERE id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", latestId.Value);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "No se pudo eliminar la configuración del blog" });

            return Ok(new
            {
                message = "Blog config eliminado",
                id = latestId.Value
            });
        }
    }
}