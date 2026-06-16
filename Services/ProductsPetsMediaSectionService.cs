using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Services
{
    public class ProductsPetsMediaSectionService
    {
        private readonly string _connectionString;

        public ProductsPetsMediaSectionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<ProductsPetsMediaSection>> GetAllAsync()
        {
            var list = new List<ProductsPetsMediaSection>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT id, title, description, image_url, video_url, sort_order, created_at
                FROM products_pets_media_sections
                ORDER BY sort_order ASC, id ASC;
            ";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        public async Task<ProductsPetsMediaSection> CreateAsync(ProductsPetsMediaSection item)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO products_pets_media_sections
                (
                    title,
                    description,
                    image_url,
                    video_url,
                    sort_order
                )
                VALUES
                (
                    @title,
                    @description,
                    @image_url,
                    @video_url,
                    @sort_order
                );

                SELECT LAST_INSERT_ID();
            ";

            using var command = new MySqlCommand(query, connection);

            AddParameters(command, item);

            var id = Convert.ToInt32(await command.ExecuteScalarAsync());
            item.Id = id;

            return item;
        }

        public async Task<bool> UpdateAsync(int id, ProductsPetsMediaSection item)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                UPDATE products_pets_media_sections
                SET
                    title = @title,
                    description = @description,
                    image_url = @image_url,
                    video_url = @video_url,
                    sort_order = @sort_order
                WHERE id = @id;
            ";

            using var command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@id", id);
            AddParameters(command, item);

            var rows = await command.ExecuteNonQueryAsync();

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                DELETE FROM products_pets_media_sections
                WHERE id = @id;
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = await command.ExecuteNonQueryAsync();

            return rows > 0;
        }

        private static void AddParameters(MySqlCommand command, ProductsPetsMediaSection item)
        {
            command.Parameters.AddWithValue("@title", item.Title ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@image_url", item.ImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@video_url", item.VideoUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@sort_order", item.SortOrder ?? 0);
        }

        private static ProductsPetsMediaSection Map(MySqlDataReader reader)
        {
            return new ProductsPetsMediaSection
            {
                Id = reader.GetInt32("id"),
                Title = reader["title"] == DBNull.Value ? null : reader.GetString("title"),
                Description = reader["description"] == DBNull.Value ? null : reader.GetString("description"),
                ImageUrl = reader["image_url"] == DBNull.Value ? null : reader.GetString("image_url"),
                VideoUrl = reader["video_url"] == DBNull.Value ? null : reader.GetString("video_url"),
                SortOrder = reader["sort_order"] == DBNull.Value ? null : reader.GetInt32("sort_order"),
                CreatedAt = reader["created_at"] == DBNull.Value ? null : reader.GetDateTime("created_at")
            };
        }
    }
}