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

        private static string? ReadNullableString(MySqlDataReader reader, string column)
            => reader[column] == DBNull.Value ? null : reader[column].ToString();

        private static object NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

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
                    
                    Price = ReadNullableString(reader, "price"),
                    Description = ReadNullableString(reader, "description"),
                    Icon = ReadNullableString(reader, "icon"),
                    ButtonTextVideo = ReadNullableString(reader, "buttonTextVideo"),
                    ButtonLinkVideo = ReadNullableString(reader, "buttonLinkVideo"),
                    ButtonTextWanted = ReadNullableString(reader, "buttonTextWanted"),
                    ButtonLinkWanted = ReadNullableString(reader, "buttonLinkWanted"),
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

            cmd.Parameters.AddWithValue("@title", NormalizeValue(req.Title));
            cmd.Parameters.AddWithValue("@price", NormalizeValue(req.Price));
            cmd.Parameters.AddWithValue("@description", NormalizeValue(req.Description));
            cmd.Parameters.AddWithValue("@icon", NormalizeValue(req.Icon));
            cmd.Parameters.AddWithValue("@buttonTextVideo", NormalizeValue(req.ButtonTextVideo));
            cmd.Parameters.AddWithValue("@buttonLinkVideo", NormalizeValue(req.ButtonLinkVideo));
            cmd.Parameters.AddWithValue("@buttonTextWanted", NormalizeValue(req.ButtonTextWanted));
            cmd.Parameters.AddWithValue("@buttonLinkWanted", NormalizeValue(req.ButtonLinkWanted));

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

            cmd.Parameters.AddWithValue("@title", NormalizeValue(req.Title));
            cmd.Parameters.AddWithValue("@price", NormalizeValue(req.Price));
            cmd.Parameters.AddWithValue("@description", NormalizeValue(req.Description));
            cmd.Parameters.AddWithValue("@icon", NormalizeValue(req.Icon));
            cmd.Parameters.AddWithValue("@buttonTextVideo", NormalizeValue(req.ButtonTextVideo));
            cmd.Parameters.AddWithValue("@buttonLinkVideo", NormalizeValue(req.ButtonLinkVideo));
            cmd.Parameters.AddWithValue("@buttonTextWanted", NormalizeValue(req.ButtonTextWanted));
            cmd.Parameters.AddWithValue("@buttonLinkWanted", NormalizeValue(req.ButtonLinkWanted));
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