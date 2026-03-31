using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly Db _db;
        public SettingsController(Db db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT * FROM general_settings LIMIT 1;", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay settings aún" });

            var data = new GeneralSettings
            {
                Id = reader.GetInt32("id"),
                Logo = reader["logo"]?.ToString() ?? string.Empty,
                PrimaryColor = reader["primary_color"]?.ToString() ?? string.Empty,
                SecondaryColor = reader["secondary_color"]?.ToString(),
                TextPrimaryColor = reader["text_primary_color"]?.ToString(),
                TextSecondaryColor = reader["text_secondary_color"]?.ToString(),
                HoverBackgroundColor = reader["hover_background_color"]?.ToString(),
                WhatsappNumber = reader["whatsapp_number"]?.ToString() ?? string.Empty,
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(data);
        }

  [HttpPost("UploadLogo")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadLogo(IFormFile logo)
{
    if (logo == null || logo.Length == 0)
        return BadRequest(new { message = "No se recibió ningún archivo." });

    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
    var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();

    if (!allowedExtensions.Contains(extension))
        return BadRequest(new { message = "Formato no permitido. Solo jpg, jpeg, png y webp." });

    var uploadsFolder = Path.Combine(
        Directory.GetCurrentDirectory(),
        "wwwroot",
        "uploads",
        "settings"
    );

    if (!Directory.Exists(uploadsFolder))
        Directory.CreateDirectory(uploadsFolder);

    var possibleOldFiles = new[]
    {
        Path.Combine(uploadsFolder, "logo.jpg"),
        Path.Combine(uploadsFolder, "logo.jpeg"),
        Path.Combine(uploadsFolder, "logo.png"),
        Path.Combine(uploadsFolder, "logo.webp")
    };

    foreach (var oldFile in possibleOldFiles)
    {
        if (System.IO.File.Exists(oldFile))
            System.IO.File.Delete(oldFile);
    }

    var fixedFileName = "logo" + extension;
    var filePath = Path.Combine(uploadsFolder, fixedFileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await logo.CopyToAsync(stream);
    }

    var baseUrl = $"{Request.Scheme}://{Request.Host}";
    var logoUrl = $"{baseUrl}/uploads/settings/{fixedFileName}";

    using var conn = _db.GetConnection();
    await conn.OpenAsync();

    using var getIdCmd = new MySqlCommand("SELECT id FROM general_settings LIMIT 1;", conn);
    var existingIdObj = await getIdCmd.ExecuteScalarAsync();

    if (existingIdObj != null)
    {
        int id = Convert.ToInt32(existingIdObj);

        using var updateCmd = new MySqlCommand(
            "UPDATE general_settings SET logo = @logo WHERE id = @id;",
            conn
        );

        updateCmd.Parameters.AddWithValue("@logo", logoUrl);
        updateCmd.Parameters.AddWithValue("@id", id);

        await updateCmd.ExecuteNonQueryAsync();

        return Ok(new
        {
            message = "Logo actualizado correctamente.",
            created = false,
            id,
            logoUrl
        });
    }

    using var insertCmd = new MySqlCommand(@"
        INSERT INTO general_settings
        (
            logo,
            primary_color,
            secondary_color,
            text_primary_color,
            text_secondary_color,
            hover_background_color,
            whatsapp_number
        )
        VALUES
        (
            @logo,
            '',
            NULL,
            NULL,
            NULL,
            NULL,
            ''
        );
        SELECT LAST_INSERT_ID();
    ", conn);

    insertCmd.Parameters.AddWithValue("@logo", logoUrl);

    int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

    return Ok(new
    {
        message = "Logo guardado correctamente.",
        created = true,
        id = newId,
        logoUrl
    });
}

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] GeneralSettingsUpdateRequest req)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id FROM general_settings LIMIT 1;", conn);
            var existingIdObj = await getIdCmd.ExecuteScalarAsync();

            if (existingIdObj != null)
            {
                int id = Convert.ToInt32(existingIdObj);

                var updateSql = @"
                    UPDATE general_settings
                    SET logo = @logo,
                        primary_color = @color,
                        secondary_color = @secondaryColor,
                        text_primary_color = @textPrimaryColor,
                        text_secondary_color = @textSecondaryColor,
                        hover_background_color = @hoverBackgroundColor,
                        whatsapp_number = @wa
                    WHERE id = @id;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@logo", req.Logo);
                cmd.Parameters.AddWithValue("@color", req.PrimaryColor);
                cmd.Parameters.AddWithValue("@secondaryColor", (object?)req.SecondaryColor ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@textPrimaryColor", (object?)req.TextPrimaryColor ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@textSecondaryColor", (object?)req.TextSecondaryColor ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@hoverBackgroundColor", (object?)req.HoverBackgroundColor ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@wa", req.WhatsappNumber);
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Settings actualizado (POST)",
                    created = false,
                    id
                });
            }

            var insertSql = @"
                INSERT INTO general_settings
                (
                    logo,
                    primary_color,
                    secondary_color,
                    text_primary_color,
                    text_secondary_color,
                    hover_background_color,
                    whatsapp_number
                )
                VALUES
                (
                    @logo,
                    @color,
                    @secondaryColor,
                    @textPrimaryColor,
                    @textSecondaryColor,
                    @hoverBackgroundColor,
                    @wa
                );
                SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@logo", req.Logo);
            insertCmd.Parameters.AddWithValue("@color", req.PrimaryColor);
            insertCmd.Parameters.AddWithValue("@secondaryColor", (object?)req.SecondaryColor ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@textPrimaryColor", (object?)req.TextPrimaryColor ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@textSecondaryColor", (object?)req.TextSecondaryColor ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@hoverBackgroundColor", (object?)req.HoverBackgroundColor ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@wa", req.WhatsappNumber);

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Settings creado (POST)",
                created = true,
                id = newId
            });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] GeneralSettingsUpdateRequest req)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id FROM general_settings LIMIT 1;", conn);
            var existingIdObj = await getIdCmd.ExecuteScalarAsync();

            if (existingIdObj == null)
            {
                return NotFound(new { message = "No existe ningún settings para actualizar." });
            }

            int id = Convert.ToInt32(existingIdObj);

            var sql = @"
                UPDATE general_settings
                SET logo = @logo,
                    primary_color = @color,
                    secondary_color = @secondaryColor,
                    text_primary_color = @textPrimaryColor,
                    text_secondary_color = @textSecondaryColor,
                    hover_background_color = @hoverBackgroundColor,
                    whatsapp_number = @wa
                WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@logo", req.Logo);
            cmd.Parameters.AddWithValue("@color", req.PrimaryColor);
            cmd.Parameters.AddWithValue("@secondaryColor", (object?)req.SecondaryColor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@textPrimaryColor", (object?)req.TextPrimaryColor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@textSecondaryColor", (object?)req.TextSecondaryColor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hoverBackgroundColor", (object?)req.HoverBackgroundColor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@wa", req.WhatsappNumber);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                message = "Settings actualizado",
                id
            });
        }
    }
}