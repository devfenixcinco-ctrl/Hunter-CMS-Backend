using System.Text.Json;
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
                HeroTitle = reader.GetString("hero_title"),
                HeroDescription = reader.IsDBNull(reader.GetOrdinal("hero_description")) ? null : reader.GetString("hero_description"),
                HeroImageUrl = reader.IsDBNull(reader.GetOrdinal("hero_image_url")) ? null : reader.GetString("hero_image_url"),

                BenefitsTitle = reader.GetString("benefits_title"),
                Benefits = DeserializeList<ProductsPetsBenefit>(reader["benefits_json"]?.ToString()),

                CreditTitle = reader.GetString("credit_title"),
                CreditSubtitle = reader.IsDBNull(reader.GetOrdinal("credit_subtitle")) ? null : reader.GetString("credit_subtitle"),
                CreditLegalText = reader.IsDBNull(reader.GetOrdinal("credit_legal_text")) ? null : reader.GetString("credit_legal_text"),
                CreditButtonText = reader.IsDBNull(reader.GetOrdinal("credit_button_text")) ? null : reader.GetString("credit_button_text"),
                CreditButtonLink = reader.IsDBNull(reader.GetOrdinal("credit_button_link")) ? null : reader.GetString("credit_button_link"),
                CreditFeatures = DeserializeList<ProductsPetsCreditFeature>(reader["credit_features_json"]?.ToString()),

                MediaSections = DeserializeList<ProductsPetsMediaSection>(reader["media_sections_json"]?.ToString()),

                PaymentTitle = reader.GetString("payment_title"),
                PrimaryButtonText = reader.IsDBNull(reader.GetOrdinal("primary_button_text")) ? null : reader.GetString("primary_button_text"),
                PrimaryButtonLink = reader.IsDBNull(reader.GetOrdinal("primary_button_link")) ? null : reader.GetString("primary_button_link"),
                SecondaryButtonText = reader.IsDBNull(reader.GetOrdinal("secondary_button_text")) ? null : reader.GetString("secondary_button_text"),
                SecondaryButtonLink = reader.IsDBNull(reader.GetOrdinal("secondary_button_link")) ? null : reader.GetString("secondary_button_link"),
                PaymentMethods = DeserializeList<ProductsPetsPaymentMethod>(reader["payment_methods_json"]?.ToString()),

                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        public async Task<int> CreateAsync(ProductsPets request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = null;

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(request.HeroImage, "productspets", "hero_image", scheme, host);
            }

            var benefitsJson = JsonSerializer.Serialize(request.Benefits);
            var creditFeaturesJson = JsonSerializer.Serialize(request.CreditFeatures);
            var mediaSectionsJson = JsonSerializer.Serialize(request.MediaSections);
            var paymentMethodsJson = JsonSerializer.Serialize(request.PaymentMethods);

            var query = @"
                INSERT INTO products_pets
                (
                    hero_title, hero_description, hero_image_url,
                    benefits_title, benefits_json,
                    credit_title, credit_subtitle, credit_legal_text, credit_button_text, credit_button_link, credit_features_json,
                    media_sections_json,
                    payment_title, primary_button_text, primary_button_link, secondary_button_text, secondary_button_link, payment_methods_json
                )
                VALUES
                (
                    @HeroTitle, @HeroDescription, @HeroImageUrl,
                    @BenefitsTitle, @BenefitsJson,
                    @CreditTitle, @CreditSubtitle, @CreditLegalText, @CreditButtonText, @CreditButtonLink, @CreditFeaturesJson,
                    @MediaSectionsJson,
                    @PaymentTitle, @PrimaryButtonText, @PrimaryButtonLink, @SecondaryButtonText, @SecondaryButtonLink, @PaymentMethodsJson
                );
                SELECT LAST_INSERT_ID();
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroDescription", (object?)request.HeroDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);

            command.Parameters.AddWithValue("@BenefitsTitle", request.BenefitsTitle);
            command.Parameters.AddWithValue("@BenefitsJson", benefitsJson);

            command.Parameters.AddWithValue("@CreditTitle", request.CreditTitle);
            command.Parameters.AddWithValue("@CreditSubtitle", (object?)request.CreditSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditLegalText", (object?)request.CreditLegalText ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditButtonText", (object?)request.CreditButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditButtonLink", (object?)request.CreditButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditFeaturesJson", creditFeaturesJson);

            command.Parameters.AddWithValue("@MediaSectionsJson", mediaSectionsJson);

            command.Parameters.AddWithValue("@PaymentTitle", request.PaymentTitle);
            command.Parameters.AddWithValue("@PrimaryButtonText", (object?)request.PrimaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@PrimaryButtonLink", (object?)request.PrimaryButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonText", (object?)request.SecondaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonLink", (object?)request.SecondaryButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@PaymentMethodsJson", paymentMethodsJson);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, ProductsPets request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = null;

            using (var getCurrent = new MySqlCommand("SELECT hero_image_url FROM products_pets WHERE id = @Id LIMIT 1;", connection))
            {
                getCurrent.Parameters.AddWithValue("@Id", id);
                var currentObj = await getCurrent.ExecuteScalarAsync();

                if (currentObj == null)
                    return false;

                heroImageUrl = currentObj == DBNull.Value ? null : currentObj.ToString();
            }

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(request.HeroImage, "productspets", "hero_image", scheme, host);
            }

            var benefitsJson = JsonSerializer.Serialize(request.Benefits);
            var creditFeaturesJson = JsonSerializer.Serialize(request.CreditFeatures);
            var mediaSectionsJson = JsonSerializer.Serialize(request.MediaSections);
            var paymentMethodsJson = JsonSerializer.Serialize(request.PaymentMethods);

            var query = @"
                UPDATE products_pets SET
                    hero_title = @HeroTitle,
                    hero_description = @HeroDescription,
                    hero_image_url = @HeroImageUrl,

                    benefits_title = @BenefitsTitle,
                    benefits_json = @BenefitsJson,

                    credit_title = @CreditTitle,
                    credit_subtitle = @CreditSubtitle,
                    credit_legal_text = @CreditLegalText,
                    credit_button_text = @CreditButtonText,
                    credit_button_link = @CreditButtonLink,
                    credit_features_json = @CreditFeaturesJson,

                    media_sections_json = @MediaSectionsJson,

                    payment_title = @PaymentTitle,
                    primary_button_text = @PrimaryButtonText,
                    primary_button_link = @PrimaryButtonLink,
                    secondary_button_text = @SecondaryButtonText,
                    secondary_button_link = @SecondaryButtonLink,
                    payment_methods_json = @PaymentMethodsJson
                WHERE id = @Id;
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroDescription", (object?)request.HeroDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);

            command.Parameters.AddWithValue("@BenefitsTitle", request.BenefitsTitle);
            command.Parameters.AddWithValue("@BenefitsJson", benefitsJson);

            command.Parameters.AddWithValue("@CreditTitle", request.CreditTitle);
            command.Parameters.AddWithValue("@CreditSubtitle", (object?)request.CreditSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditLegalText", (object?)request.CreditLegalText ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditButtonText", (object?)request.CreditButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditButtonLink", (object?)request.CreditButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@CreditFeaturesJson", creditFeaturesJson);

            command.Parameters.AddWithValue("@MediaSectionsJson", mediaSectionsJson);

            command.Parameters.AddWithValue("@PaymentTitle", request.PaymentTitle);
            command.Parameters.AddWithValue("@PrimaryButtonText", (object?)request.PrimaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@PrimaryButtonLink", (object?)request.PrimaryButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonText", (object?)request.SecondaryButtonText ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryButtonLink", (object?)request.SecondaryButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@PaymentMethodsJson", paymentMethodsJson);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static List<T> DeserializeList<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            try
            {
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
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