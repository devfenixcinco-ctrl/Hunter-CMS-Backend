using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MreHunter.Api.Data;
using MreHunter.Api.Models;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/contact-header")]
    [Tags("Formulario Contacto Header")]
    public class ContactInfoController : ControllerBase
    {
        private readonly Db _db;

        public ContactInfoController(Db db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT *
                FROM contact_info
                ORDER BY id DESC
                LIMIT 1;", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay información de contacto registrada." });

            return Ok(new
            {
                id = reader.GetInt32("id"),
                phone = reader["phone"]?.ToString(),
                whatsappText = reader["whatsapp_text"]?.ToString(),
                whatsappLink = reader["whatsapp_link"]?.ToString(),
                email = reader["email"]?.ToString(),
                createdAt = reader["created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["created_at"]),
                updatedAt = reader["updated_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["updated_at"])
            });
        }

        [Authorize]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactInfoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            long? existingId = null;

            using (var find = new MySqlCommand("SELECT id FROM contact_info ORDER BY id DESC LIMIT 1;", conn))
            {
                var obj = await find.ExecuteScalarAsync();
                if (obj != null)
                    existingId = Convert.ToInt64(obj);
            }

            if (existingId.HasValue)
            {
                using var updateCmd = new MySqlCommand(@"
                    UPDATE contact_info
                    SET
                        phone = @phone,
                        whatsapp_text = @whatsapp_text,
                        whatsapp_link = @whatsapp_link,
                        email = @email,
                        updated_at = NOW()
                    WHERE id = @id;", conn);

                updateCmd.Parameters.AddWithValue("@phone", (object?)request.Phone ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@whatsapp_text", (object?)request.WhatsappText ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@whatsapp_link", (object?)request.WhatsappLink ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@email", (object?)request.Email ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("@id", existingId.Value);

                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Información de contacto actualizada.",
                    id = existingId.Value,
                    created = false
                });
            }

            using var insertCmd = new MySqlCommand(@"
                INSERT INTO contact_info
                (
                    phone,
                    whatsapp_text,
                    whatsapp_link,
                    email
                )
                VALUES
                (
                    @phone,
                    @whatsapp_text,
                    @whatsapp_link,
                    @email
                );
                SELECT LAST_INSERT_ID();", conn);

            insertCmd.Parameters.AddWithValue("@phone", (object?)request.Phone ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@whatsapp_text", (object?)request.WhatsappText ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@whatsapp_link", (object?)request.WhatsappLink ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@email", (object?)request.Email ?? DBNull.Value);

            var newId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Información de contacto creada.",
                id = newId,
                created = true
            });
}

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ContactInfoRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            long? id = null;
            using (var find = new MySqlCommand("SELECT id FROM contact_info ORDER BY id DESC LIMIT 1;", conn))
            {
                var obj = await find.ExecuteScalarAsync();
                if (obj == null)
                    return NotFound(new { message = "No existe información de contacto para actualizar." });

                id = Convert.ToInt64(obj);
            }

            using var cmd = new MySqlCommand(@"
                UPDATE contact_info
                SET
                    phone = @phone,
                    whatsapp_text = @whatsapp_text,
                    whatsapp_link = @whatsapp_link,
                    email = @email,
                    updated_at = NOW()
                WHERE id = @id;", conn);

            cmd.Parameters.AddWithValue("@phone", (object?)request.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@whatsapp_text", (object?)request.WhatsappText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@whatsapp_link", (object?)request.WhatsappLink ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)request.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id.Value);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                message = "Información de contacto actualizada.",
                id
            });
        }
    }
}