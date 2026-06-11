using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/payment-methods")]
    [Tags("Métodos de Pago")]
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
                string.IsNullOrWhiteSpace(req.Price) ||
                string.IsNullOrWhiteSpace(req.Description) ||
                string.IsNullOrWhiteSpace(req.Icon);
        }
                

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"
                SELECT 
                    id,
                    title,
                    price,
                    description,
                    icon,
                    buttonTextVideo,
                    buttonLinkVideo,
                    buttonTextWanted,
                    buttonLinkWanted,
                    created_at
                FROM payment_methods
                ORDER BY id ASC;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<PaymentMethod>();

            while (await reader.ReadAsync())
            {
                items.Add(new PaymentMethod
                {
                    Id = reader.GetInt32("id"),
                    Title = reader["title"]?.ToString(),
                    Price = reader["price"]?.ToString(),
                    Description = reader["description"]?.ToString(),
                    Icon = reader["icon"]?.ToString(),
                    ButtonTextVideo = reader["buttonTextVideo"]?.ToString(),
                    ButtonLinkVideo = reader["buttonLinkVideo"]?.ToString(),
                    ButtonTextWanted = reader["buttonTextWanted"]?.ToString(),
                    ButtonLinkWanted = reader["buttonLinkWanted"]?.ToString(),
                    CreatedAt = reader.GetDateTime("created_at")
                });
            }

            return Ok(items);
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
                    message = "Title, Price, Description, Icon, ButtonTextVideo, ButtonLinkVideo, ButtonTextWanted y ButtonLinkWanted son obligatorios."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO payment_methods
                (
                    title,
                    price,
                    description,
                    icon,
                    buttonTextVideo,
                    buttonLinkVideo,
                    buttonTextWanted,
                    buttonLinkWanted
                )
                VALUES
                (
                    @title,
                    @price,
                    @description,
                    @icon,
                    @buttonTextVideo,
                    @buttonLinkVideo,
                    @buttonTextWanted,
                    @buttonLinkWanted
                );
                SELECT LAST_INSERT_ID();
            ";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            cmd.Parameters.AddWithValue("@price", req.Price?.Trim());
            cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
            cmd.Parameters.AddWithValue("@icon", req.Icon?.Trim());
            cmd.Parameters.AddWithValue("@buttonTextVideo", req.ButtonTextVideo?.Trim());
            cmd.Parameters.AddWithValue("@buttonLinkVideo", req.ButtonLinkVideo?.Trim());
            cmd.Parameters.AddWithValue("@buttonTextWanted", req.ButtonTextWanted?.Trim());
            cmd.Parameters.AddWithValue("@buttonLinkWanted", req.ButtonLinkWanted?.Trim());

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
                    message = "Title, Price, Description, Icon, ButtonTextVideo, ButtonLinkVideo, ButtonTextWanted y ButtonLinkWanted son obligatorios."
                });
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"
                UPDATE payment_methods
                SET 
                    title = @title,
                    price = @price,
                    description = @description,
                    icon = @icon,
                    buttonTextVideo = @buttonTextVideo,
                    buttonLinkVideo = @buttonLinkVideo,
                    buttonTextWanted = @buttonTextWanted,
                    buttonLinkWanted = @buttonLinkWanted
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@title", req.Title?.Trim());
            cmd.Parameters.AddWithValue("@price", req.Price?.Trim());
            cmd.Parameters.AddWithValue("@description", req.Description?.Trim());
            cmd.Parameters.AddWithValue("@icon", req.Icon?.Trim());
            cmd.Parameters.AddWithValue("@buttonTextVideo", req.ButtonTextVideo?.Trim());
            cmd.Parameters.AddWithValue("@buttonLinkVideo", req.ButtonLinkVideo?.Trim());
            cmd.Parameters.AddWithValue("@buttonTextWanted", req.ButtonTextWanted?.Trim());
            cmd.Parameters.AddWithValue("@buttonLinkWanted", req.ButtonLinkWanted?.Trim());
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