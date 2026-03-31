using MySqlConnector;
using MreHunter.Api.Models;

namespace MreHunter.Api.Services
{
    public class CompanyService
    {
        private readonly IConfiguration _configuration;

        public CompanyService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<CompanyResponse?> GetCompanyAsync()
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            CompanyPage? page = null;
            CompanyStat? stats = null;

            var pageQuery = @"SELECT * 
                              FROM company_page
                              ORDER BY id ASC
                              LIMIT 1;";

            using (var command = new MySqlCommand(pageQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    page = new CompanyPage
                    {
                        Id = reader.GetInt32("id"),
                        HeroTitle = reader.GetString("hero_title"),
                        HeroSubtitle = reader.IsDBNull(reader.GetOrdinal("hero_subtitle")) ? null : reader.GetString("hero_subtitle"),
                        HeroImageUrl = reader.IsDBNull(reader.GetOrdinal("hero_image_url")) ? null : reader.GetString("hero_image_url"),
                        VideoTitle = reader.GetString("video_title"),
                        VideoSubtitle = reader.IsDBNull(reader.GetOrdinal("video_subtitle")) ? null : reader.GetString("video_subtitle"),
                        VideoUrl = reader.IsDBNull(reader.GetOrdinal("video_url")) ? null : reader.GetString("video_url"),
                        CtaTitle = reader.GetString("cta_title"),
                        CtaSubtitle = reader.IsDBNull(reader.GetOrdinal("cta_subtitle")) ? null : reader.GetString("cta_subtitle"),
                        CtaButton1Text = reader.GetString("cta_button_1_text"),
                        CtaButton1Link = reader.IsDBNull(reader.GetOrdinal("cta_button_1_link")) ? null : reader.GetString("cta_button_1_link"),
                        CtaButton2Text = reader.GetString("cta_button_2_text"),
                        CtaButton2Link = reader.IsDBNull(reader.GetOrdinal("cta_button_2_link")) ? null : reader.GetString("cta_button_2_link"),
                        CreatedAt = reader.GetDateTime("created_at"),
                        UpdatedAt = reader.GetDateTime("updated_at")
                    };
                }
            }

            if (page == null)
                return null;

            var statsQuery = @"SELECT * 
                               FROM company_stats
                               WHERE company_page_id = @CompanyPageId
                               ORDER BY id ASC
                               LIMIT 1;";

            using (var command = new MySqlCommand(statsQuery, connection))
            {
                command.Parameters.AddWithValue("@CompanyPageId", page.Id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    stats = new CompanyStat
                    {
                        Id = reader.GetInt32("id"),
                        CompanyPageId = reader.GetInt32("company_page_id"),
                        UsersCount = reader.GetString("users_count"),
                        UsersLabel = reader.GetString("users_label"),
                        RecoveriesCount = reader.GetString("recoveries_count"),
                        RecoveriesLabel = reader.GetString("recoveries_label"),
                        CapturesCount = reader.GetString("captures_count"),
                        CapturesLabel = reader.GetString("captures_label"),
                        CreatedAt = reader.GetDateTime("created_at")
                    };
                }
            }

            return new CompanyResponse
            {
                Page = page,
                Stats = stats
            };
        }

        public async Task<int> CreateCompanyPageAsync(CompanyPage request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = null;

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(request.HeroImage, "company", "hero_image", scheme, host);
            }

            var query = @"INSERT INTO company_page
                          (hero_title, hero_subtitle, hero_image_url,
                           video_title, video_subtitle, video_url,
                           cta_title, cta_subtitle,
                           cta_button_1_text, cta_button_1_link,
                           cta_button_2_text, cta_button_2_link)
                          VALUES
                          (@HeroTitle, @HeroSubtitle, @HeroImageUrl,
                           @VideoTitle, @VideoSubtitle, @VideoUrl,
                           @CtaTitle, @CtaSubtitle,
                           @CtaButton1Text, @CtaButton1Link,
                           @CtaButton2Text, @CtaButton2Link);
                          SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroSubtitle", (object?)request.HeroSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@VideoTitle", request.VideoTitle);
            command.Parameters.AddWithValue("@VideoSubtitle", (object?)request.VideoSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@VideoUrl", (object?)request.VideoUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@CtaTitle", request.CtaTitle);
            command.Parameters.AddWithValue("@CtaSubtitle", (object?)request.CtaSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CtaButton1Text", request.CtaButton1Text);
            command.Parameters.AddWithValue("@CtaButton1Link", (object?)request.CtaButton1Link ?? DBNull.Value);
            command.Parameters.AddWithValue("@CtaButton2Text", request.CtaButton2Text);
            command.Parameters.AddWithValue("@CtaButton2Link", (object?)request.CtaButton2Link ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateCompanyPageAsync(int id, CompanyPage request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string? heroImageUrl = null;

            using (var getCurrent = new MySqlCommand("SELECT hero_image_url FROM company_page WHERE id = @Id LIMIT 1;", connection))
            {
                getCurrent.Parameters.AddWithValue("@Id", id);
                var currentObj = await getCurrent.ExecuteScalarAsync();

                if (currentObj == null)
                    return false;

                heroImageUrl = currentObj == DBNull.Value ? null : currentObj.ToString();
            }

            if (request.HeroImage != null)
            {
                heroImageUrl = await SaveImage(request.HeroImage, "company", "hero_image", scheme, host);
            }

            var query = @"UPDATE company_page SET
                            hero_title = @HeroTitle,
                            hero_subtitle = @HeroSubtitle,
                            hero_image_url = @HeroImageUrl,
                            video_title = @VideoTitle,
                            video_subtitle = @VideoSubtitle,
                            video_url = @VideoUrl,
                            cta_title = @CtaTitle,
                            cta_subtitle = @CtaSubtitle,
                            cta_button_1_text = @CtaButton1Text,
                            cta_button_1_link = @CtaButton1Link,
                            cta_button_2_text = @CtaButton2Text,
                            cta_button_2_link = @CtaButton2Link
                          WHERE id = @Id;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroSubtitle", (object?)request.HeroSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)heroImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@VideoTitle", request.VideoTitle);
            command.Parameters.AddWithValue("@VideoSubtitle", (object?)request.VideoSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@VideoUrl", (object?)request.VideoUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@CtaTitle", request.CtaTitle);
            command.Parameters.AddWithValue("@CtaSubtitle", (object?)request.CtaSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CtaButton1Text", request.CtaButton1Text);
            command.Parameters.AddWithValue("@CtaButton1Link", (object?)request.CtaButton1Link ?? DBNull.Value);
            command.Parameters.AddWithValue("@CtaButton2Text", request.CtaButton2Text);
            command.Parameters.AddWithValue("@CtaButton2Link", (object?)request.CtaButton2Link ?? DBNull.Value);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> CreateCompanyStatsAsync(CompanyStat request)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO company_stats
                          (company_page_id, users_count, users_label,
                           recoveries_count, recoveries_label,
                           captures_count, captures_label)
                          VALUES
                          (@CompanyPageId, @UsersCount, @UsersLabel,
                           @RecoveriesCount, @RecoveriesLabel,
                           @CapturesCount, @CapturesLabel);
                          SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@CompanyPageId", request.CompanyPageId);
            command.Parameters.AddWithValue("@UsersCount", request.UsersCount);
            command.Parameters.AddWithValue("@UsersLabel", request.UsersLabel);
            command.Parameters.AddWithValue("@RecoveriesCount", request.RecoveriesCount);
            command.Parameters.AddWithValue("@RecoveriesLabel", request.RecoveriesLabel);
            command.Parameters.AddWithValue("@CapturesCount", request.CapturesCount);
            command.Parameters.AddWithValue("@CapturesLabel", request.CapturesLabel);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateCompanyStatsAsync(int id, CompanyStat request)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE company_stats SET
                            company_page_id = @CompanyPageId,
                            users_count = @UsersCount,
                            users_label = @UsersLabel,
                            recoveries_count = @RecoveriesCount,
                            recoveries_label = @RecoveriesLabel,
                            captures_count = @CapturesCount,
                            captures_label = @CapturesLabel
                          WHERE id = @Id;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@CompanyPageId", request.CompanyPageId);
            command.Parameters.AddWithValue("@UsersCount", request.UsersCount);
            command.Parameters.AddWithValue("@UsersLabel", request.UsersLabel);
            command.Parameters.AddWithValue("@RecoveriesCount", request.RecoveriesCount);
            command.Parameters.AddWithValue("@RecoveriesLabel", request.RecoveriesLabel);
            command.Parameters.AddWithValue("@CapturesCount", request.CapturesCount);
            command.Parameters.AddWithValue("@CapturesLabel", request.CapturesLabel);

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