using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using MreHunter.Api.Data;
using MreHunter.Api.Models;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactMessageController : ControllerBase
    {
        private readonly Db _db;

        public ContactMessageController(Db db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactMessageRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest(new { message = "El nombre es obligatorio." });

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "El mensaje es obligatorio." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                INSERT INTO contact_messages
                (
                    first_name,
                    last_name,
                    phone,
                    email,
                    message
                )
                VALUES
                (
                    @first_name,
                    @last_name,
                    @phone,
                    @email,
                    @message
                );
                SELECT LAST_INSERT_ID();", conn);

            cmd.Parameters.AddWithValue("@first_name", request.FirstName);
            cmd.Parameters.AddWithValue("@last_name", (object?)request.LastName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", (object?)request.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)request.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@message", request.Message);

            var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Mensaje enviado correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT *
                FROM contact_messages
                WHERE id = @id;", conn);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "Mensaje no encontrado." });

           return Ok(new
            {
                id = reader.GetInt32("id"),
                firstName = reader["first_name"]?.ToString(),
                lastName = reader["last_name"]?.ToString(),
                phone = reader["phone"]?.ToString(),
                email = reader["email"]?.ToString(),
                message = reader["message"]?.ToString(),
                status = reader["status"]?.ToString(),
                createdAt = reader["created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["created_at"])
            });
        }

       
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "El status es obligatorio." });

            var allowedStatuses = new[] { "new", "read", "answered" };

            if (!allowedStatuses.Contains(status.ToLower()))
                return BadRequest(new { message = "Status no válido. Usa: new, read o answered." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                UPDATE contact_messages
                SET status = @status
                WHERE id = @id;", conn);

            cmd.Parameters.AddWithValue("@status", status.ToLower());
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Mensaje no encontrado." });

            return Ok(new
            {
                message = "Estado del mensaje actualizado.",
                id,
                status = status.ToLower()
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()

        {

            var result = new List<object>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT *
                FROM contact_messages
                ORDER BY id DESC;", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
              result.Add(new
            {
                id = reader.GetInt32("id"),
                firstName = reader["first_name"]?.ToString(),
                lastName = reader["last_name"]?.ToString(),
                phone = reader["phone"]?.ToString(),
                email = reader["email"]?.ToString(),
                message = reader["message"]?.ToString(),
                status = reader["status"]?.ToString(),
                createdAt = reader["created_at"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["created_at"])
            });
            }

            return Ok(result);
        }
    }
}