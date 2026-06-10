using Microsoft.AspNetCore.Http;
using MySqlConnector;
using MreHunter.Api.Models;
using System.IO;

namespace MreHunter.Api.Services
{
    public class ProductsPortableService
    {
        private readonly IConfiguration _configuration;

        public ProductsPortableService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<ProductsPortable?> GetAsync()
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM products_portable ORDER BY id ASC LIMIT 1;";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new ProductsPortable
            {
                Id = reader.GetInt32("id"),
                HeroTitle = reader.GetString("hero_title"),
                HeroSubtitle = reader.IsDBNull(reader.GetOrdinal("hero_subtitle"))
                    ? null
                    : reader.GetString("hero_subtitle"),
                HeroHighlightText = reader.IsDBNull(reader.GetOrdinal("hero_highlight_text"))
                    ? null
                    : reader.GetString("hero_highlight_text"),
                HeroImageUrl = reader.IsDBNull(reader.GetOrdinal("hero_image_url"))
                    ? null
                    : reader.GetString("hero_image_url"),

                ReasonTitle = reader.GetString("reason_title"),
                ReasonDescription = reader.IsDBNull(reader.GetOrdinal("reason_description"))
                    ? null
                    : reader.GetString("reason_description"),

                PaymentTitle = reader.GetString("payment_title"),
                PrimaryButtonText = reader.IsDBNull(reader.GetOrdinal("primary_button_text"))
                    ? null
                    : reader.GetString("primary_button_text"),
                PrimaryButtonLink = reader.IsDBNull(reader.GetOrdinal("primary_button_link"))
                    ? null
                    : reader.GetString("primary_button_link"),
                SecondaryButtonText = reader.IsDBNull(reader.GetOrdinal("secondary_button_text"))
                    ? null
                    : reader.GetString("secondary_button_text"),
                SecondaryButtonLink = reader.IsDBNull(reader.GetOrdinal("secondary_button_link"))
                    ? null
                    : reader.GetString("secondary_button_link"),

                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        public async Task<int> CreateAsync(ProductsPortable request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = null;

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(
                    request.HeroImage,
                    "productsportable",
                    "hero_image",
                    scheme,
                    host
                );
            }

            var query = @"
                INSERT INTO products_portable
                (
                    hero_title,
                    hero_subtitle,
                    hero_highlight_text,
                    hero_image_url,
                    reason_title,
                    reason_description,
                    payment_title,
                    primary_button_text,
                    primary_button_link,
                    secondary_button_text,
                    secondary_button_link
                )
                VALUES
                (
                    @HeroTitle,
                    @HeroSubtitle,
                    @HeroHighlightText,
                    @HeroImageUrl,
                    @ReasonTitle,
                    @ReasonDescription,
                    @PaymentTitle,
                    @PrimaryButtonText,
                    @PrimaryButtonLink,
                    @SecondaryButtonText,
                    @SecondaryButtonLink
                );
                SELECT LAST_INSERT_ID();
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroSubtitle", (object?)request.HeroSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroHighlightText", (object?)request.HeroHighlightText ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);

            command.Parameters.AddWithValue("@ReasonTitle", request.ReasonTitle);
            command.Parameters.AddWithValue("@ReasonDescription", (object?)request.ReasonDescription ?? DBNull.Value);

            command.Parameters.AddWithValue("@PaymentTitle", request.PaymentTitle);
            command.Parameters.AddWithValue("@PrimaryButtonText", (object?)request.PrimaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@PrimaryButtonLink", (object?)request.PrimaryButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonText", (object?)request.SecondaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonLink", (object?)request.SecondaryButtonLink ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, ProductsPortable request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = null;

            using (var getCurrent = new MySqlCommand(
                "SELECT hero_image_url FROM products_portable WHERE id = @Id LIMIT 1;",
                connection))
            {
                getCurrent.Parameters.AddWithValue("@Id", id);
                var currentObj = await getCurrent.ExecuteScalarAsync();

                if (currentObj == null)
                    return false;

                heroImageUrl = currentObj == DBNull.Value ? null : currentObj.ToString();
            }

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(
                    request.HeroImage,
                    "productsportable",
                    "hero_image",
                    scheme,
                    host
                );
            }

            var query = @"
                UPDATE products_portable SET
                    hero_title = @HeroTitle,
                    hero_subtitle = @HeroSubtitle,
                    hero_highlight_text = @HeroHighlightText,
                    hero_image_url = @HeroImageUrl,
                    reason_title = @ReasonTitle,
                    reason_description = @ReasonDescription,
                    payment_title = @PaymentTitle,
                    primary_button_text = @PrimaryButtonText,
                    primary_button_link = @PrimaryButtonLink,
                    secondary_button_text = @SecondaryButtonText,
                    secondary_button_link = @SecondaryButtonLink
                WHERE id = @Id;
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroSubtitle", (object?)request.HeroSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroHighlightText", (object?)request.HeroHighlightText ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);

            command.Parameters.AddWithValue("@ReasonTitle", request.ReasonTitle);
            command.Parameters.AddWithValue("@ReasonDescription", (object?)request.ReasonDescription ?? DBNull.Value);

            command.Parameters.AddWithValue("@PaymentTitle", request.PaymentTitle);
            command.Parameters.AddWithValue("@PrimaryButtonText", (object?)request.PrimaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@PrimaryButtonLink", (object?)request.PrimaryButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonText", (object?)request.SecondaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonLink", (object?)request.SecondaryButtonLink ?? DBNull.Value);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private async Task<string> SaveImage(
            IFormFile file,
            string folder,
            string fileNameWithoutExtension,
            string scheme,
            string host)
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

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"{scheme}://{host}/uploads/{folder}/{finalFileName}";
        }
    }
}