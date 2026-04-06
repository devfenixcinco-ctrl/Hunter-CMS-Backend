using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/conventional-benefits")]
    [Tags("Producto Convencional (Beneficios)")]
    public class ProductBenefitItemController : ControllerBase
    {
        private readonly Db _db;

        public ProductBenefitItemController(Db db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, title, icon, created_at
                        FROM benefit_items
                        ORDER BY id ASC;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<ProductBenefitItem>();

            while (await reader.ReadAsync())
            {
                items.Add(new ProductBenefitItem
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Icon = reader["icon"]?.ToString(),
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

            var sql = @"SELECT id, title, icon, created_at
                        FROM benefit_items
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "Beneficio no encontrado." });

            var item = new ProductBenefitItem
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Icon = reader["icon"]?.ToString(),
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductBenefitItemCreateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO benefit_items (title, icon)
                        VALUES (@title, @icon);
                        SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@icon", (object?)req.Icon ?? DBNull.Value);

            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return Ok(new
            {
                message = "Beneficio creado correctamente.",
                id = newId
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductBenefitItemUpdateRequest req)
        {
            if (req == null)
                return BadRequest(new { message = "Body requerido." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE benefit_items
                        SET title = @title,
                            icon = @icon
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@icon", (object?)req.Icon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Beneficio no encontrado." });

            return Ok(new { message = "Beneficio actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "DELETE FROM benefit_items WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows == 0)
                return NotFound(new { message = "Beneficio no encontrado." });

            return Ok(new { message = "Beneficio eliminado correctamente." });
        }
    }
}