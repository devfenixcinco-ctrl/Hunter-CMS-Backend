using MySqlConnector;
using MreHunter.Api.Models;

namespace MreHunter.Api.Services
{
    public class DashcamBenefitsService
    {
        private readonly IConfiguration _configuration;

        public DashcamBenefitsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<DashcamBenefit>> GetAllAsync()
        {
            var list = new List<DashcamBenefit>();

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "SELECT * FROM dashcam_benefits;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new DashcamBenefit
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Icon = reader.IsDBNull(reader.GetOrdinal("icon"))
                        ? null
                        : reader.GetString("icon"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }

            return list;
        }

        public async Task<DashcamBenefit?> GetByIdAsync(int id)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "SELECT * FROM dashcam_benefits WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new DashcamBenefit
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Icon = reader.IsDBNull(reader.GetOrdinal("icon"))
                    ? null
                    : reader.GetString("icon"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        public async Task<int> CreateAsync(DashcamBenefitRequest req)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = @"INSERT INTO dashcam_benefits
                        (title, icon)
                        VALUES
                        (@title, @icon);
                        SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@icon", (object?)req.Icon ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, DashcamBenefitRequest req)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = @"UPDATE dashcam_benefits
                        SET
                            title = @title,
                            icon = @icon,
                            updated_at = NOW()
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@icon", (object?)req.Icon ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "DELETE FROM dashcam_benefits WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}