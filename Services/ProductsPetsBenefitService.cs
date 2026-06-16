using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Services
{
    public class ProductsPetsBenefitService
    {
        private readonly string _connectionString;

        public ProductsPetsBenefitService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<ProductsPetsBenefit>> GetAllAsync()
        {
            var benefits = new List<ProductsPetsBenefit>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT * FROM products_pets_benefits ORDER BY sort_order ASC, id ASC;";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                benefits.Add(new ProductsPetsBenefit
                {
                    Id = reader.GetInt32("id"),
                    Icon = reader["icon"]?.ToString(),
                    Title = reader["title"]?.ToString(),
                    Subtitle = reader["subtitle"]?.ToString(),
                    SortOrder = reader.GetInt32("sort_order")
                });
            }

            return benefits;
        }

        public async Task<int> CreateAsync(ProductsPetsBenefit request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                INSERT INTO products_pets_benefits
                (
                    icon,
                    title,
                    subtitle,
                    sort_order
                )
                VALUES
                (
                    @icon,
                    @title,
                    @subtitle,
                    @sort_order
                );

                SELECT LAST_INSERT_ID();
            ";

            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@icon", (object?)request.Icon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", (object?)request.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@subtitle", (object?)request.Subtitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sort_order", request.SortOrder);

            var result = await cmd.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, ProductsPetsBenefit request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                UPDATE products_pets_benefits SET
                    icon = @icon,
                    title = @title,
                    subtitle = @subtitle,
                    sort_order = @sort_order
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@icon", (object?)request.Icon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", (object?)request.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@subtitle", (object?)request.Subtitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sort_order", request.SortOrder);

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "DELETE FROM products_pets_benefits WHERE id = @id;";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }
    }
}