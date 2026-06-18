using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Services
{
    public class PlanPlusBenefitService
    {
        private readonly string _connectionString;

        public PlanPlusBenefitService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<PlanPlusBenefit>> GetAllAsync()
        {
            var benefits = new List<PlanPlusBenefit>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT * FROM plan_plus_benefits ORDER BY id DESC;";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                benefits.Add(new PlanPlusBenefit
                {
                    Id = reader.GetInt32("id"),
                    Title = reader["title"]?.ToString(),
                    Icon = reader["icon"]?.ToString()
                });
            }

            return benefits;
        }

        public async Task<int> CreateAsync(PlanPlusBenefit request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                INSERT INTO plan_plus_benefits (
                    title,
                    icon
                )
                VALUES (
                    @title,
                    @icon
                );

                SELECT LAST_INSERT_ID();
            ";

            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@title", request.Title);
            cmd.Parameters.AddWithValue("@icon", request.Icon);

            var result = await cmd.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }


      public async Task<bool> UpdateAsync(int id, PlanPlusBenefit request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                UPDATE plan_plus_benefits SET
                    title = @title,
                    icon = @icon
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@title", request.Title);
            cmd.Parameters.AddWithValue("@icon", request.Icon);

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "DELETE FROM plan_plus_benefits WHERE id = @id;";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }
    }
}