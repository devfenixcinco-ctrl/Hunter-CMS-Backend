using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Services
{
    public class GpsSectionService
    {
        private readonly string _connectionString;

        public GpsSectionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<GpsSection?> GetAsync()
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT 
                    id,
                    why_title,
                    why_description,
                    video_title_1,
                    video_description_1,
                    video_url_1,
                    video_image_1,
                    video_title_2,
                    video_description_2,
                    video_url_2,
                    video_image_2,
                    product_title,
                    product_description,
                    product_price,
                    product_icon,
                    button_text,
                    button_url
                FROM gps_section
                ORDER BY id ASC
                LIMIT 1;
            ";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new GpsSection
            {
                Id = reader.GetInt32("id"),
                WhyTitle = reader["why_title"]?.ToString(),
                WhyDescription = reader["why_description"]?.ToString(),

                VideoTitle1 = reader["video_title_1"]?.ToString(),
                VideoDescription1 = reader["video_description_1"]?.ToString(),
                VideoUrl1 = reader["video_url_1"]?.ToString(),
                VideoImage1 = reader["video_image_1"]?.ToString(),

                VideoTitle2 = reader["video_title_2"]?.ToString(),
                VideoDescription2 = reader["video_description_2"]?.ToString(),
                VideoUrl2 = reader["video_url_2"]?.ToString(),
                VideoImage2 = reader["video_image_2"]?.ToString(),

                ProductTitle = reader["product_title"]?.ToString(),
                ProductDescription = reader["product_description"]?.ToString(),
                ProductPrice = reader["product_price"]?.ToString(),
                ProductIcon = reader["product_icon"]?.ToString(),

                ButtonText = reader["button_text"]?.ToString(),
                ButtonUrl = reader["button_url"]?.ToString()
            };
        }

        public async Task<bool> UpdateAsync(GpsSection request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                UPDATE gps_section SET
                    why_title = @why_title,
                    why_description = @why_description,

                    video_title_1 = @video_title_1,
                    video_description_1 = @video_description_1,
                    video_url_1 = @video_url_1,
                    video_image_1 = @video_image_1,

                    video_title_2 = @video_title_2,
                    video_description_2 = @video_description_2,
                    video_url_2 = @video_url_2,
                    video_image_2 = @video_image_2,

                    product_title = @product_title,
                    product_description = @product_description,
                    product_price = @product_price,
                    product_icon = @product_icon,

                    button_text = @button_text,
                    button_url = @button_url
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@id", request.Id);

            cmd.Parameters.AddWithValue("@why_title", request.WhyTitle);
            cmd.Parameters.AddWithValue("@why_description", request.WhyDescription);

            cmd.Parameters.AddWithValue("@video_title_1", request.VideoTitle1);
            cmd.Parameters.AddWithValue("@video_description_1", request.VideoDescription1);
            cmd.Parameters.AddWithValue("@video_url_1", request.VideoUrl1);
            cmd.Parameters.AddWithValue("@video_image_1", request.VideoImage1);

            cmd.Parameters.AddWithValue("@video_title_2", request.VideoTitle2);
            cmd.Parameters.AddWithValue("@video_description_2", request.VideoDescription2);
            cmd.Parameters.AddWithValue("@video_url_2", request.VideoUrl2);
            cmd.Parameters.AddWithValue("@video_image_2", request.VideoImage2);

            cmd.Parameters.AddWithValue("@product_title", request.ProductTitle);
            cmd.Parameters.AddWithValue("@product_description", request.ProductDescription);
            cmd.Parameters.AddWithValue("@product_price", request.ProductPrice);
            cmd.Parameters.AddWithValue("@product_icon", request.ProductIcon);

            cmd.Parameters.AddWithValue("@button_text", request.ButtonText);
            cmd.Parameters.AddWithValue("@button_url", request.ButtonUrl);

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                DELETE FROM gps_section
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }
    }
}