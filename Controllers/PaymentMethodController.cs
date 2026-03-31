using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodController : ControllerBase
    {
        private readonly Db _db;

        public PaymentMethodController(Db db)
        {
            _db = db;
        }

        private bool HasInvalidRequiredFields(PaymentMethodRequest req)
        {
            return string.IsNullOrWhiteSpace(req.Title) ||
                   string.IsNullOrWhiteSpace(req.Description) ||
                   string.IsNullOrWhiteSpace(req.Icon) ||
                   string.IsNullOrWhiteSpace(req.ButtonText) ||
                   string.IsNullOrWhiteSpace(req.ButtonLink) ||
                   req.DisplayOrder <= 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, title, description, icon, button_text, button_link, display_order, is_active, created_at
                        FROM payment_methods
                        ORDER BY display_order ASC, id ASC;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<PaymentMethod>();

            while (await reader.ReadAsync())
            {
                items.Add(new PaymentMethod
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Description = reader["description"]?.ToString(),
                    Icon = reader["icon"]?.ToString(),
                    ButtonText = reader["button_text"]?.ToString(),
                    ButtonLink = reader["button_link"]?.ToString(),
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

            var sql = @"SELECT id, title, description, icon, button_text, button_link, display_order, is_active, created_at
                        FROM payment_methods
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "Método de pago no encontrado." });

            var item = new PaymentMethod
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Description = reader["description"]?.ToString(),
                Icon = reader["icon"]?.ToString(),
                ButtonText = reader["button_text"]?.ToString(),
                ButtonLink = reader["button_link"]?.ToString(),
                DisplayOrder = reader.GetInt32("display_order"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(item);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentMethodRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(req))
            {
                return BadRequest(new
                {
                    message = "Title, Description, Icon, ButtonText y ButtonLink son obligatorios y DisplayOrder debe ser mayor a 0."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO payment_methods
                        (title, description, icon, button_text, button_link, display_order, is_active)
                        VALUES
                        (@title, @description, @icon, @buttonText, @buttonLink, @displayOrder, @isActive);
                        SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
            cmd.Parameters.AddWithValue("@icon", req.Icon?.Trim());
            cmd.Parameters.AddWithValue("@buttonText", req.ButtonText?.Trim());
            cmd.Parameters.AddWithValue("@buttonLink", req.ButtonLink?.Trim());
            cmd.Parameters.AddWithValue("@displayOrder", req.DisplayOrder);
            cmd.Parameters.AddWithValue("@isActive", req.IsActive);

            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Método de pago creado correctamente.",
                id = newId
            });
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PaymentMethodRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (HasInvalidRequiredFields(req))
            {
                return BadRequest(new
                {
                    message = "Title, Description, Icon, ButtonText y ButtonLink son obligatorios y DisplayOrder debe ser mayor a 0."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE payment_methods
                        SET title = @title,
                            description = @description,
                            icon = @icon,
                            button_text = @buttonText,
                            button_link = @buttonLink,
                            display_order = @displayOrder,
                            is_active = @isActive
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
            cmd.Parameters.AddWithValue("@icon", req.Icon?.Trim());
            cmd.Parameters.AddWithValue("@buttonText", req.ButtonText?.Trim());
            cmd.Parameters.AddWithValue("@buttonLink", req.ButtonLink?.Trim());
            cmd.Parameters.AddWithValue("@displayOrder", req.DisplayOrder);
            cmd.Parameters.AddWithValue("@isActive", req.IsActive);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Método de pago no encontrado." });

            return Ok(new { message = "Método de pago actualizado correctamente." });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "DELETE FROM payment_methods WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Método de pago no encontrado." });

            return Ok(new { message = "Método de pago eliminado correctamente." });
        }
    }
}