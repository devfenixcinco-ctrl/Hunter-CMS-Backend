using Microsoft.AspNetCore.Http;
using MySqlConnector;
using MreHunter.Api.Models;

namespace MreHunter.Api.Services
{
    public class PortableMediaSectionsService
    {
        private readonly IConfiguration _configuration;

        public PortableMediaSectionsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<PortableMediaSection>> GetAllAsync()
        {
            var list = new List<PortableMediaSection>();

            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "SELECT * FROM portable_media_sections;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new PortableMediaSection
                {
                    Id = reader.GetInt32("id"),
                    Title = reader.GetString("title"),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString("description"),
                    VideoUrl = reader.IsDBNull(reader.GetOrdinal("video_url"))
                        ? null
                        : reader.GetString("video_url"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }

            return list;
        }

        public async Task<PortableMediaSection?> GetByIdAsync(int id)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "SELECT * FROM portable_media_sections WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new PortableMediaSection
            {
                Id = reader.GetInt32("id"),
                Title = reader.GetString("title"),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null
                    : reader.GetString("description"),
                VideoUrl = reader.IsDBNull(reader.GetOrdinal("video_url"))
                    ? null
                    : reader.GetString("video_url"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        public async Task<int> CreateAsync(PortableMediaSectionRequest req, string scheme, string host)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql =  @"INSERT INTO portable_media_sections
            (title, description, video_url)
            VALUES
            (@title, @description, @videoUrl);
            SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@videoUrl", (object?)req.VideoUrl ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, PortableMediaSectionRequest req)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = @"UPDATE portable_media_sections
                        SET
                            title = @title,
                            description = @description,
                            video_url = @videoUrl,
                            updated_at = NOW()
                        WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@title", req.Title);
            cmd.Parameters.AddWithValue("@description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@videoUrl", (object?)req.VideoUrl ?? DBNull.Value);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var sql = "DELETE FROM portable_media_sections WHERE id = @id;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}