using MySqlConnector;
using MreHunter.Api.Models;

namespace MreHunter.Api.Services
{
    public class PortableBenefitsService
    {
        private readonly IConfiguration _configuration;

        public PortableBenefitsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<PortableBenefit>> GetAllAsync()
        {
            var list = new List<PortableBenefit>();

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "SELECT * FROM portable_benefits;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new PortableBenefit
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Icon = reader.GetString("icon"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }

            return list;
        }

        public async Task<PortableBenefit?> GetByIdAsync(int id)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "SELECT * FROM portable_benefits WHERE id = @id LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new PortableBenefit
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Icon = reader.GetString("icon"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        public async Task<int> CreateAsync(PortableBenefitRequest req)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = @"INSERT INTO portable_benefits (title, icon)
                        VALUES (@title, @icon);
                        SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@icon", req.Icon);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, PortableBenefitRequest req)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = @"UPDATE portable_benefits
                        SET title = @title,
                            icon = @icon
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@icon", req.Icon);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "DELETE FROM portable_benefits WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}