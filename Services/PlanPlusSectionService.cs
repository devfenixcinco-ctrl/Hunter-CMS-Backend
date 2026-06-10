using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Services
{
    public class PlanPlusSectionService
    {
        private readonly string _connectionString;

        public PlanPlusSectionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<PlanPlusSection>> GetAllAsync()
        {
            var sections = new List<PlanPlusSection>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT * FROM plan_plus_section ORDER BY id DESC;";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                sections.Add(Map(reader));
            }

            return sections;
        }

        public async Task<PlanPlusSection?> GetByIdAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT * FROM plan_plus_section WHERE id = @id;";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return Map(reader);
        }

        public async Task<int> CreateAsync(PlanPlusSection request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                INSERT INTO plan_plus_section (
                    hero_badge, hero_title, hero_subtitle,
                    hero_left_image, hero_right_image,
                    benefits_title,
                    benefit_1, benefit_2, benefit_3, benefit_4, benefit_5, benefit_6,
                    quote_title, quote_subtitle, quote_card_title, quote_note, quote_button_text,
                    video_title_1, video_description_1, video_url_1, video_image_1,
                    video_title_2, video_description_2, video_url_2, video_image_2,
                    product_section_title, product_title, product_description, product_icon,
                    button_text_1, button_url_1, button_text_2, button_url_2
                ) VALUES (
                    @hero_badge, @hero_title, @hero_subtitle,
                    @hero_left_image, @hero_right_image,
                    @benefits_title,
                    @benefit_1, @benefit_2, @benefit_3, @benefit_4, @benefit_5, @benefit_6,
                    @quote_title, @quote_subtitle, @quote_card_title, @quote_note, @quote_button_text,
                    @video_title_1, @video_description_1, @video_url_1, @video_image_1,
                    @video_title_2, @video_description_2, @video_url_2, @video_image_2,
                    @product_section_title, @product_title, @product_description, @product_icon,
                    @button_text_1, @button_url_1, @button_text_2, @button_url_2
                );
                SELECT LAST_INSERT_ID();
            ";

            using var cmd = new MySqlCommand(query, conn);
            AddParameters(cmd, request);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateAsync(int id, PlanPlusSection request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                UPDATE plan_plus_section SET
                    hero_badge = @hero_badge,
                    hero_title = @hero_title,
                    hero_subtitle = @hero_subtitle,
                    hero_left_image = @hero_left_image,
                    hero_right_image = @hero_right_image,
                    benefits_title = @benefits_title,
                    benefit_1 = @benefit_1,
                    benefit_2 = @benefit_2,
                    benefit_3 = @benefit_3,
                    benefit_4 = @benefit_4,
                    benefit_5 = @benefit_5,
                    benefit_6 = @benefit_6,
                    quote_title = @quote_title,
                    quote_subtitle = @quote_subtitle,
                    quote_card_title = @quote_card_title,
                    quote_note = @quote_note,
                    quote_button_text = @quote_button_text,
                    video_title_1 = @video_title_1,
                    video_description_1 = @video_description_1,
                    video_url_1 = @video_url_1,
                    video_image_1 = @video_image_1,
                    video_title_2 = @video_title_2,
                    video_description_2 = @video_description_2,
                    video_url_2 = @video_url_2,
                    video_image_2 = @video_image_2,
                    product_section_title = @product_section_title,
                    product_title = @product_title,
                    product_description = @product_description,
                    product_icon = @product_icon,
                    button_text_1 = @button_text_1,
                    button_url_1 = @button_url_1,
                    button_text_2 = @button_text_2,
                    button_url_2 = @button_url_2
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            AddParameters(cmd, request);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "DELETE FROM plan_plus_section WHERE id = @id;";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static void AddParameters(MySqlCommand cmd, PlanPlusSection request)
        {
            cmd.Parameters.AddWithValue("@hero_badge", request.HeroBadge);
            cmd.Parameters.AddWithValue("@hero_title", request.HeroTitle);
            cmd.Parameters.AddWithValue("@hero_subtitle", request.HeroSubtitle);
            cmd.Parameters.AddWithValue("@hero_left_image", request.HeroLeftImage);
            cmd.Parameters.AddWithValue("@hero_right_image", request.HeroRightImage);

            cmd.Parameters.AddWithValue("@benefits_title", request.BenefitsTitle);
            cmd.Parameters.AddWithValue("@benefit_1", request.Benefit1);
            cmd.Parameters.AddWithValue("@benefit_2", request.Benefit2);
            cmd.Parameters.AddWithValue("@benefit_3", request.Benefit3);
            cmd.Parameters.AddWithValue("@benefit_4", request.Benefit4);
            cmd.Parameters.AddWithValue("@benefit_5", request.Benefit5);
            cmd.Parameters.AddWithValue("@benefit_6", request.Benefit6);

            cmd.Parameters.AddWithValue("@quote_title", request.QuoteTitle);
            cmd.Parameters.AddWithValue("@quote_subtitle", request.QuoteSubtitle);
            cmd.Parameters.AddWithValue("@quote_card_title", request.QuoteCardTitle);
            cmd.Parameters.AddWithValue("@quote_note", request.QuoteNote);
            cmd.Parameters.AddWithValue("@quote_button_text", request.QuoteButtonText);

            cmd.Parameters.AddWithValue("@video_title_1", request.VideoTitle1);
            cmd.Parameters.AddWithValue("@video_description_1", request.VideoDescription1);
            cmd.Parameters.AddWithValue("@video_url_1", request.VideoUrl1);
            cmd.Parameters.AddWithValue("@video_image_1", request.VideoImage1);

            cmd.Parameters.AddWithValue("@video_title_2", request.VideoTitle2);
            cmd.Parameters.AddWithValue("@video_description_2", request.VideoDescription2);
            cmd.Parameters.AddWithValue("@video_url_2", request.VideoUrl2);
            cmd.Parameters.AddWithValue("@video_image_2", request.VideoImage2);

            cmd.Parameters.AddWithValue("@product_section_title", request.ProductSectionTitle);
            cmd.Parameters.AddWithValue("@product_title", request.ProductTitle);
            cmd.Parameters.AddWithValue("@product_description", request.ProductDescription);
            cmd.Parameters.AddWithValue("@product_icon", request.ProductIcon);

            cmd.Parameters.AddWithValue("@button_text_1", request.ButtonText1);
            cmd.Parameters.AddWithValue("@button_url_1", request.ButtonUrl1);
            cmd.Parameters.AddWithValue("@button_text_2", request.ButtonText2);
            cmd.Parameters.AddWithValue("@button_url_2", request.ButtonUrl2);
        }

        private static PlanPlusSection Map(MySqlDataReader reader)
        {
            return new PlanPlusSection
            {
                Id = reader.GetInt32("id"),

                HeroBadge = reader["hero_badge"]?.ToString(),
                HeroTitle = reader["hero_title"]?.ToString(),
                HeroSubtitle = reader["hero_subtitle"]?.ToString(),
                HeroLeftImage = reader["hero_left_image"]?.ToString(),
                HeroRightImage = reader["hero_right_image"]?.ToString(),

                BenefitsTitle = reader["benefits_title"]?.ToString(),
                Benefit1 = reader["benefit_1"]?.ToString(),
                Benefit2 = reader["benefit_2"]?.ToString(),
                Benefit3 = reader["benefit_3"]?.ToString(),
                Benefit4 = reader["benefit_4"]?.ToString(),
                Benefit5 = reader["benefit_5"]?.ToString(),
                Benefit6 = reader["benefit_6"]?.ToString(),

                QuoteTitle = reader["quote_title"]?.ToString(),
                QuoteSubtitle = reader["quote_subtitle"]?.ToString(),
                QuoteCardTitle = reader["quote_card_title"]?.ToString(),
                QuoteNote = reader["quote_note"]?.ToString(),
                QuoteButtonText = reader["quote_button_text"]?.ToString(),

                VideoTitle1 = reader["video_title_1"]?.ToString(),
                VideoDescription1 = reader["video_description_1"]?.ToString(),
                VideoUrl1 = reader["video_url_1"]?.ToString(),
                VideoImage1 = reader["video_image_1"]?.ToString(),

                VideoTitle2 = reader["video_title_2"]?.ToString(),
                VideoDescription2 = reader["video_description_2"]?.ToString(),
                VideoUrl2 = reader["video_url_2"]?.ToString(),
                VideoImage2 = reader["video_image_2"]?.ToString(),

                ProductSectionTitle = reader["product_section_title"]?.ToString(),
                ProductTitle = reader["product_title"]?.ToString(),
                ProductDescription = reader["product_description"]?.ToString(),
                ProductIcon = reader["product_icon"]?.ToString(),

                ButtonText1 = reader["button_text_1"]?.ToString(),
                ButtonUrl1 = reader["button_url_1"]?.ToString(),
                ButtonText2 = reader["button_text_2"]?.ToString(),
                ButtonUrl2 = reader["button_url_2"]?.ToString()
            };
        }
    }
}