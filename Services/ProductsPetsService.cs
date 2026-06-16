using Microsoft.AspNetCore.Http;
using MySqlConnector;
using MreHunter.Api.Models;

namespace MreHunter.Api.Services
{
    public class ProductsPetsService
    {
        private readonly IConfiguration _configuration;

        public ProductsPetsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<ProductsPets?> GetAsync()
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"SELECT * FROM products_pets ORDER BY id ASC LIMIT 1;";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new ProductsPets
            {
                Id = reader.GetInt32("id"),
                HeroTitle = reader["hero_title"]?.ToString(),
                HeroDescription = reader["hero_description"]?.ToString(),
                HeroImageUrl = reader["hero_image_url"]?.ToString(),
                BenefitsTitle = reader["benefits_title"]?.ToString(),
                CardTitle = reader["card_title"]?.ToString(),
                CardText = reader["card_text"]?.ToString(),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        public async Task<int> CreateAsync(ProductsPets request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = request.HeroImageUrl;

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(request.HeroImage, "productspets", "hero_image", scheme, host);
            }

            var query = @"
                INSERT INTO products_pets
                (
                    hero_title,
                    hero_description,
                    hero_image_url,
                    benefits_title,
                    card_title,
                    card_text
                )
                VALUES
                (
                    @HeroTitle,
                    @HeroDescription,
                    @HeroImageUrl,
                    @BenefitsTitle,
                    @CardTitle,
                    @CardText
                );
                SELECT LAST_INSERT_ID();
            ";

            using var command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@HeroTitle", (object?)request.HeroTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroDescription", (object?)request.HeroDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@BenefitsTitle", (object?)request.BenefitsTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardTitle", (object?)request.CardTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardText", (object?)request.CardText ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, ProductsPets request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = request.HeroImageUrl;

            using (var getCurrent = new MySqlCommand("SELECT hero_image_url FROM products_pets WHERE id = @Id LIMIT 1;", connection))
            {
                getCurrent.Parameters.AddWithValue("@Id", id);
                var currentObj = await getCurrent.ExecuteScalarAsync();

                if (currentObj == null)
                    return false;

                if (string.IsNullOrWhiteSpace(heroImageUrl))
                    heroImageUrl = currentObj == DBNull.Value ? null : currentObj.ToString();
            }

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(request.HeroImage, "productspets", "hero_image", scheme, host);
            }

            var query = @"
                UPDATE products_pets SET
                    hero_title = @HeroTitle,
                    hero_description = @HeroDescription,
                    hero_image_url = @HeroImageUrl,
                    benefits_title = @BenefitsTitle,
                    card_title = @CardTitle,
                    card_text = @CardText
                WHERE id = @Id;
            ";

            using var command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@HeroTitle", (object?)request.HeroTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroDescription", (object?)request.HeroDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@BenefitsTitle", (object?)request.BenefitsTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardTitle", (object?)request.CardTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CardText", (object?)request.CardText ?? DBNull.Value);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private async Task<string> SaveImage(IFormFile file, string folder, string fileNameWithoutExtension, string scheme, string host)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No se recibió ningún archivo.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new Exception("Formato no permitido. Solo jpg, jpeg, png y webp.");

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                folder
            );

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var ext in allowedExtensions)
            {
                var oldFile = Path.Combine(uploadsFolder, fileNameWithoutExtension + ext);
                if (File.Exists(oldFile))
                    File.Delete(oldFile);
            }

            var finalFileName = fileNameWithoutExtension + extension;
            var filePath = Path.Combine(uploadsFolder, finalFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{scheme}://{host}/uploads/{folder}/{finalFileName}";
        }
    }
}